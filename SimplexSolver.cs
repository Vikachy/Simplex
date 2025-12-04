using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimplexSolver
{
    public class SimplexSolution
    {
        public bool IsOptimal { get; set; }
        public bool IsFeasible { get; set; }
        public bool IsUnbounded { get; set; }
        public double[] Solution { get; set; }
        public double OptimalValue { get; set; }
        public double[] SlackVariables { get; set; }
        public List<SimplexTable> Tables { get; set; } = new List<SimplexTable>();
        public string Log { get; set; }
    }

    public class SimplexMethod
    {
        private StringBuilder log = new StringBuilder();
        private const double EPSILON = 1e-10;

        public SimplexSolution Solve(LinearProgrammingProblem problem)
        {
            log.Clear();
            log.AppendLine("=== РЕШЕНИЕ ЗАДАЧИ ЛИНЕЙНОГО ПРОГРАММИРОВАНИЯ ===");
            log.AppendLine();

            try
            {
                // Шаг 1: Приведение к канонической форме
                log.AppendLine("1. Приведение к канонической форме:");
                var canonicalForm = ConvertToCanonicalForm(problem);

                // Шаг 2: Построение начальной симплекс-таблицы
                log.AppendLine("\n2. Построение начальной симплекс-таблицы:");
                var solution = BuildInitialTable(canonicalForm);

                // Шаг 3: Итерации симплекс-метода
                log.AppendLine("\n3. Итерации симплекс-метода:");
                PerformSimplexIterations(solution, canonicalForm);

                // Шаг 4: Извлечение решения
                ExtractSolution(solution, canonicalForm, problem);

                solution.Log = log.ToString();
                return solution;
            }
            catch (Exception ex)
            {
                log.AppendLine($"Ошибка при решении: {ex.Message}");
                return new SimplexSolution
                {
                    Log = log.ToString(),
                    IsFeasible = false
                };
            }
        }

        private (double[,] A, double[] b, double[] c, bool[] isArtificial,
                  int nVars, int nSlack, int nArtificial)
            ConvertToCanonicalForm(LinearProgrammingProblem problem)
        {
            int nVariables = problem.ObjectiveCoefficients.Count;
            int nConstraints = problem.Constraints.Count;

            // Подсчитываем количество дополнительных переменных
            int slackVars = 0;
            int artificialVars = 0;

            foreach (var constraint in problem.Constraints)
            {
                if (constraint.TypeIndex == 0) // <=
                {
                    slackVars++;
                }
                else if (constraint.TypeIndex == 1) // =
                {
                    artificialVars++;
                }
                else if (constraint.TypeIndex == 2) // >=
                {
                    slackVars++;
                    artificialVars++;
                }
            }

            int totalVars = nVariables + slackVars + artificialVars;

            // Создаем матрицу A и векторы b, c
            double[,] A = new double[nConstraints, totalVars];
            double[] b = new double[nConstraints];
            double[] c = new double[totalVars];
            bool[] isArtificial = new bool[totalVars];

            // Заполняем коэффициенты целевой функции
            for (int i = 0; i < nVariables; i++)
            {
                c[i] = problem.IsMaximization ? -problem.ObjectiveCoefficients[i].Value
                                             : problem.ObjectiveCoefficients[i].Value;
            }

            // Для искусственных переменных в целевой функции ставим большую цену (M-метод)
            for (int i = nVariables + slackVars; i < totalVars; i++)
            {
                c[i] = problem.IsMaximization ? double.MaxValue / 1000 : -double.MaxValue / 1000;
            }

            // Индексы для дополнительных переменных
            int slackIndex = nVariables;
            int artificialIndex = nVariables + slackVars;
            int currentSlack = 0;
            int currentArtificial = 0;

            log.AppendLine($"   Всего переменных: {totalVars} " +
                          $"(основные: {nVariables}, " +
                          $"дополнительные: {slackVars}, " +
                          $"искусственные: {artificialVars})");

            // Заполняем ограничения
            for (int i = 0; i < nConstraints; i++)
            {
                var constraint = problem.Constraints[i];
                b[i] = constraint.RightHandSide;

                // Основные переменные
                for (int j = 0; j < nVariables; j++)
                {
                    A[i, j] = constraint.Coefficients[j].Value;
                }

                // Дополнительные и искусственные переменные
                switch (constraint.TypeIndex)
                {
                    case 0: // <=
                        A[i, slackIndex + currentSlack] = 1;
                        log.AppendLine($"   Ограничение {i + 1} (&lt;=): добавлена " +
                                      $"дополнительная переменная x{slackIndex + currentSlack + 1} с коэф. +1");
                        currentSlack++;
                        break;

                    case 1: // =
                        A[i, artificialIndex + currentArtificial] = 1;
                        isArtificial[artificialIndex + currentArtificial] = true;
                        log.AppendLine($"   Ограничение {i + 1} (=): добавлена " +
                                      $"искусственная переменная x{artificialIndex + currentArtificial + 1} с коэф. +1");
                        currentArtificial++;
                        break;

                    case 2: // >=
                        A[i, slackIndex + currentSlack] = -1;
                        log.AppendLine($"   Ограничение {i + 1} (&gt;=): добавлена " +
                                      $"дополнительная переменная x{slackIndex + currentSlack + 1} с коэф. -1");

                        A[i, artificialIndex + currentArtificial] = 1;
                        isArtificial[artificialIndex + currentArtificial] = true;
                        log.AppendLine($"   Ограничение {i + 1} (&gt;=): добавлена " +
                                      $"искусственная переменная x{artificialIndex + currentArtificial + 1} с коэф. +1");

                        currentSlack++;
                        currentArtificial++;
                        break;
                }
            }

            return (A, b, c, isArtificial, nVariables, slackVars, artificialVars);
        }

        private SimplexSolution BuildInitialTable(
            (double[,] A, double[] b, double[] c, bool[] isArtificial,
             int nVars, int nSlack, int nArtificial) canonicalForm)
        {
            int m = canonicalForm.b.Length; // количество ограничений
            int n = canonicalForm.c.Length; // всего переменных

            var solution = new SimplexSolution
            {
                Tables = new List<SimplexTable>()
            };

            // Создаем начальную таблицу
            var initialTable = new SimplexTable
            {
                TableName = "Начальная симплекс-таблица"
            };

            // Определяем начальный базис (искусственные переменные имеют приоритет)
            int[] basis = new int[m];
            double[] cB = new double[m];

            // Сначала размещаем искусственные переменные в базисе
            int basisCount = 0;
            for (int j = canonicalForm.nVars + canonicalForm.nSlack;
                 j < n && basisCount < m; j++)
            {
                for (int i = 0; i < m; i++)
                {
                    if (Math.Abs(canonicalForm.A[i, j] - 1) < EPSILON)
                    {
                        bool isUnitColumn = true;
                        for (int k = 0; k < m; k++)
                        {
                            if (k != i && Math.Abs(canonicalForm.A[k, j]) > EPSILON)
                            {
                                isUnitColumn = false;
                                break;
                            }
                        }

                        if (isUnitColumn && !basis.Contains(i))
                        {
                            basis[i] = j;
                            cB[i] = canonicalForm.c[j];
                            basisCount++;
                            break;
                        }
                    }
                }
            }

            // Затем дополнительные переменные
            for (int j = canonicalForm.nVars;
                 j < canonicalForm.nVars + canonicalForm.nSlack && basisCount < m; j++)
            {
                for (int i = 0; i < m; i++)
                {
                    if (Math.Abs(canonicalForm.A[i, j] - 1) < EPSILON)
                    {
                        bool isUnitColumn = true;
                        for (int k = 0; k < m; k++)
                        {
                            if (k != i && Math.Abs(canonicalForm.A[k, j]) > EPSILON)
                            {
                                isUnitColumn = false;
                                break;
                            }
                        }

                        if (isUnitColumn && !basis.Contains(i))
                        {
                            basis[i] = j;
                            cB[i] = canonicalForm.c[j];
                            basisCount++;
                            break;
                        }
                    }
                }
            }

            log.AppendLine($"   Базисные переменные:");
            for (int i = 0; i < m; i++)
            {
                log.AppendLine($"     x{basis[i] + 1} (строка {i + 1})");
            }

            // Заполняем строки таблицы
            for (int i = 0; i < m; i++)
            {
                var row = new SimplexRow
                {
                    BasisVariable = $"x{basis[i] + 1}",
                    CB = cB[i],
                    RightHandSide = canonicalForm.b[i]
                };

                for (int j = 0; j < n; j++)
                {
                    row.Coefficients.Add(canonicalForm.A[i, j]);
                }

                initialTable.Rows.Add(row);
            }

            // Добавляем F-строку
            var fRow = new SimplexRow
            {
                BasisVariable = "F",
                CB = 0
            };

            // Вычисляем оценки Δj = cj - Σ(cBi * aij)
            for (int j = 0; j < n; j++)
            {
                double delta = canonicalForm.c[j];
                for (int i = 0; i < m; i++)
                {
                    delta -= cB[i] * canonicalForm.A[i, j];
                }
                fRow.Coefficients.Add(delta);
            }

            // Вычисляем значение F
            double fValue = 0;
            for (int i = 0; i < m; i++)
            {
                fValue += cB[i] * canonicalForm.b[i];
            }
            fRow.RightHandSide = fValue;

            initialTable.Rows.Add(fRow);
            solution.Tables.Add(initialTable);

            log.AppendLine($"   Размер таблицы: {m}×{n}");
            log.AppendLine($"   Начальное значение F = {fValue:F2}");

            return solution;
        }

        private void PerformSimplexIterations(
            SimplexSolution solution,
            (double[,] A, double[] b, double[] c, bool[] isArtificial,
             int nVars, int nSlack, int nArtificial) canonicalForm)
        {
            int iteration = 0;
            int maxIterations = 100; // защита от бесконечного цикла

            while (iteration < maxIterations)
            {
                iteration++;
                log.AppendLine($"\n   Итерация {iteration}:");

                var currentTable = solution.Tables.Last();
                int m = currentTable.Rows.Count - 1; // без F-строки
                int n = currentTable.Rows[0].Coefficients.Count;

                // Получаем последнюю строку (F-строку)
                var fRow = currentTable.Rows[m];

                // Проверка оптимальности для максимизации (все Δj >= 0)
                // Для минимизации: все Δj <= 0
                bool isOptimal = true;
                int pivotColumn = -1;
                double maxNegative = 0;

                for (int j = 0; j < n; j++)
                {
                    double delta = fRow.Coefficients[j];

                    // Для максимизации ищем отрицательные оценки
                    // Для минимизации ищем положительные
                    if (delta < -EPSILON)
                    {
                        isOptimal = false;
                        if (delta < maxNegative)
                        {
                            maxNegative = delta;
                            pivotColumn = j;
                        }
                    }
                }

                if (isOptimal)
                {
                    log.AppendLine("   Решение оптимально!");
                    solution.IsOptimal = true;

                    // Проверяем наличие искусственных переменных в базисе
                    bool hasArtificialInBasis = false;
                    for (int i = 0; i < m; i++)
                    {
                        int varIndex = GetVariableIndex(currentTable.Rows[i].BasisVariable);
                        if (varIndex >= canonicalForm.nVars + canonicalForm.nSlack)
                        {
                            hasArtificialInBasis = true;
                            break;
                        }
                    }

                    if (hasArtificialInBasis && Math.Abs(fRow.RightHandSide) > EPSILON * 1000)
                    {
                        log.AppendLine("   В базисе остались искусственные переменные с ненулевыми значениями!");
                        log.AppendLine("   Задача не имеет допустимого решения!");
                        solution.IsFeasible = false;
                    }
                    else
                    {
                        solution.IsFeasible = true;
                    }

                    return;
                }

                if (pivotColumn == -1)
                {
                    log.AppendLine("   Не удалось найти разрешающий столбец!");
                    break;
                }

                log.AppendLine($"   Разрешающий столбец: x{pivotColumn + 1} (Δ = {maxNegative:F4})");

                // Находим разрешающую строку по минимальному симплексному отношению
                int pivotRow = -1;
                double minRatio = double.MaxValue;

                for (int i = 0; i < m; i++)
                {
                    double aij = currentTable.Rows[i].Coefficients[pivotColumn];
                    if (aij > EPSILON)
                    {
                        double ratio = currentTable.Rows[i].RightHandSide / aij;
                        if (ratio < minRatio - EPSILON)
                        {
                            minRatio = ratio;
                            pivotRow = i;
                        }
                    }
                }

                if (pivotRow == -1)
                {
                    log.AppendLine("   Нет положительных коэффициентов в разрешающем столбце!");
                    log.AppendLine("   Задача неограничена!");
                    solution.IsUnbounded = true;
                    return;
                }

                log.AppendLine($"   Разрешающая строка: строка {pivotRow + 1} (θ = {minRatio:F4})");
                log.AppendLine($"   Разрешающий элемент: a[{pivotRow + 1},{pivotColumn + 1}] = " +
                              $"{currentTable.Rows[pivotRow].Coefficients[pivotColumn]:F4}");

                // Создаем новую таблицу
                var newTable = new SimplexTable
                {
                    TableName = $"Симплекс-таблица {iteration + 1}"
                };

                double pivotElement = currentTable.Rows[pivotRow].Coefficients[pivotColumn];

                // Заполняем новую таблицу
                for (int i = 0; i <= m; i++) // включая F-строку
                {
                    var newRow = new SimplexRow
                    {
                        BasisVariable = currentTable.Rows[i].BasisVariable,
                        CB = currentTable.Rows[i].CB,
                        RightHandSide = 0
                    };

                    // Копируем коэффициенты
                    for (int j = 0; j < n; j++)
                    {
                        newRow.Coefficients.Add(0);
                    }

                    newTable.Rows.Add(newRow);
                }

                // Обновляем базисную переменную в разрешающей строке
                newTable.Rows[pivotRow].BasisVariable = $"x{pivotColumn + 1}";
                newTable.Rows[pivotRow].CB = canonicalForm.c[pivotColumn];

                // Пересчитываем разрешающую строку
                for (int j = 0; j < n; j++)
                {
                    newTable.Rows[pivotRow].Coefficients[j] =
                        currentTable.Rows[pivotRow].Coefficients[j] / pivotElement;
                }
                newTable.Rows[pivotRow].RightHandSide =
                    currentTable.Rows[pivotRow].RightHandSide / pivotElement;

                // Пересчитываем остальные строки
                for (int i = 0; i <= m; i++)
                {
                    if (i == pivotRow) continue;

                    double factor = currentTable.Rows[i].Coefficients[pivotColumn];

                    for (int j = 0; j < n; j++)
                    {
                        newTable.Rows[i].Coefficients[j] =
                            currentTable.Rows[i].Coefficients[j] -
                            factor * newTable.Rows[pivotRow].Coefficients[j];
                    }

                    newTable.Rows[i].RightHandSide =
                        currentTable.Rows[i].RightHandSide -
                        factor * newTable.Rows[pivotRow].RightHandSide;
                }

                solution.Tables.Add(newTable);

                // Вычисляем новое значение F
                double newFValue = 0;
                for (int i = 0; i < m; i++)
                {
                    newFValue += newTable.Rows[i].CB * newTable.Rows[i].RightHandSide;
                }
                newTable.Rows[m].RightHandSide = newFValue;

                log.AppendLine($"   Новое значение F = {newFValue:F4}");
            }

            if (iteration >= maxIterations)
            {
                log.AppendLine("   Превышено максимальное количество итераций!");
            }
        }

        private int GetVariableIndex(string varName)
        {
            if (varName.StartsWith("x"))
            {
                string num = varName.Substring(1);
                if (int.TryParse(num, out int index))
                {
                    return index - 1;
                }
            }
            return -1;
        }

        private void ExtractSolution(
            SimplexSolution solution,
            (double[,] A, double[] b, double[] c, bool[] isArtificial,
             int nVars, int nSlack, int nArtificial) canonicalForm,
            LinearProgrammingProblem originalProblem)
        {
            if (!solution.IsOptimal || !solution.IsFeasible)
            {
                log.AppendLine("\n4. Решение не найдено или не оптимально!");
                return;
            }

            var finalTable = solution.Tables.Last();
            int m = finalTable.Rows.Count - 1; // без F-строки
            int nVars = canonicalForm.nVars;

            // Извлекаем значения переменных
            double[] solutionVars = new double[nVars];
            double[] slackVars = new double[canonicalForm.nSlack];

            // Инициализируем нулями
            for (int i = 0; i < nVars; i++) solutionVars[i] = 0;
            for (int i = 0; i < canonicalForm.nSlack; i++) slackVars[i] = 0;

            // Заполняем значения базисных переменных
            for (int i = 0; i < m; i++)
            {
                int varIndex = GetVariableIndex(finalTable.Rows[i].BasisVariable);
                if (varIndex >= 0)
                {
                    double value = finalTable.Rows[i].RightHandSide;

                    if (varIndex < nVars)
                    {
                        solutionVars[varIndex] = value;
                    }
                    else if (varIndex < nVars + canonicalForm.nSlack)
                    {
                        slackVars[varIndex - nVars] = value;
                    }
                    // Искусственные переменные игнорируем
                }
            }

            solution.Solution = solutionVars;
            solution.SlackVariables = slackVars;
            solution.OptimalValue = finalTable.Rows[m].RightHandSide;

            // Корректируем знак для максимизации/минимизации
            if (originalProblem.IsMaximization)
            {
                solution.OptimalValue = -solution.OptimalValue;
            }

            log.AppendLine("\n4. Извлечение решения:");
            log.AppendLine($"   Оптимальное значение: F = {solution.OptimalValue:F2}");

            log.AppendLine($"   Значения переменных:");
            for (int i = 0; i < nVars; i++)
            {
                log.AppendLine($"     x{i + 1} = {solutionVars[i]:F2}");
            }

            if (canonicalForm.nSlack > 0)
            {
                log.AppendLine($"   Значения дополнительных переменных:");
                for (int i = 0; i < canonicalForm.nSlack; i++)
                {
                    log.AppendLine($"     s{i + 1} = {slackVars[i]:F2}");
                }
            }

            // Анализ использования ресурсов
            log.AppendLine($"\n5. Анализ использования ресурсов:");
            for (int i = 0; i < originalProblem.Constraints.Count; i++)
            {
                var constraint = originalProblem.Constraints[i];
                double used = 0;

                for (int j = 0; j < nVars; j++)
                {
                    used += solutionVars[j] * constraint.Coefficients[j].Value;
                }

                double remaining = constraint.RightHandSide - used;

                log.AppendLine($"   {constraint.Name}:");
                log.AppendLine($"     Использовано: {used:F2} из {constraint.RightHandSide:F2}");
                log.AppendLine($"     Осталось: {remaining:F2}");

                if (Math.Abs(remaining) < EPSILON)
                {
                    log.AppendLine($"     Ресурс использован полностью");
                }
                else if (remaining > 0)
                {
                    log.AppendLine($"     Ресурс использован не полностью");
                }
            }
        }
    }
}