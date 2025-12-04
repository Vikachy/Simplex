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
        public List<double> OriginalObjective { get; set; }
    }

    public class SimplexMethod
    {
        private StringBuilder log = new StringBuilder();
        private const double EPSILON = 1e-10;
        private List<double> originalObjective;

        public SimplexSolution Solve(LinearProgrammingProblem problem)
        {
            log.Clear();
            originalObjective = problem.ObjectiveCoefficients.Select(c => c.Value).ToList();

            log.AppendLine("=== РЕШЕНИЕ ЗАДАЧИ ЛИНЕЙНОГО ПРОГРАММИРОВАНИЯ ===");
            log.AppendLine();

            try
            {
                // Шаг 1: Приведение к канонической форме
                log.AppendLine("1. Приведение к канонической форме:");
                var canonicalForm = ConvertToCanonicalForm(problem);

                // Шаг 2: Построение начальной симплекс-таблицы
                log.AppendLine("\n2. Построение начальной симплекс-таблицы:");
                var solution = BuildInitialTable(canonicalForm, problem);

                // Шаг 3: Итерации симплекс-метода
                log.AppendLine("\n3. Итерации симплекс-метода:");
                if (solution.Tables.Count > 0)
                {
                    PerformSimplexIterations(solution, canonicalForm, problem);
                }

                // Шаг 4: Извлечение решения
                ExtractSolution(solution, canonicalForm, problem);

                solution.OriginalObjective = originalObjective;
                solution.Log = log.ToString();
                return solution;
            }
            catch (Exception ex)
            {
                log.AppendLine($"Ошибка при решении: {ex.Message}");
                log.AppendLine($"StackTrace: {ex.StackTrace}");
                return new SimplexSolution
                {
                    Log = log.ToString(),
                    IsFeasible = false
                };
            }
        }

        private (double[,] A, double[] b, double[] c, bool[] isArtificial,
                  int nVars, int nSlack, int nArtificial, string[] varNames)
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

            // Создаем имена переменных
            string[] varNames = new string[totalVars];
            for (int i = 0; i < nVariables; i++)
            {
                varNames[i] = $"x{i + 1}";
            }
            for (int i = 0; i < slackVars; i++)
            {
                varNames[nVariables + i] = $"s{i + 1}";
            }
            for (int i = 0; i < artificialVars; i++)
            {
                varNames[nVariables + slackVars + i] = $"a{i + 1}";
            }

            // Создаем матрицу A и векторы b, c
            double[,] A = new double[nConstraints, totalVars];
            double[] b = new double[nConstraints];
            double[] c = new double[totalVars];
            bool[] isArtificial = new bool[totalVars];

            // Заполняем коэффициенты целевой функции
            for (int i = 0; i < nVariables; i++)
            {
                // Для максимизации используем -c, для минимизации +c
                // Но для отображения в таблице будем менять знак в F-строке
                c[i] = problem.IsMaximization ? -problem.ObjectiveCoefficients[i].Value
                                             : problem.ObjectiveCoefficients[i].Value;
            }

            // Для искусственных переменных в целевой функции ставим большую цену (M-метод)
            double bigM = problem.IsMaximization ? -1e6 : 1e6;
            for (int i = nVariables + slackVars; i < totalVars; i++)
            {
                c[i] = bigM;
                isArtificial[i] = true;
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
                                      $"дополнительная переменная s{currentSlack + 1}");
                        currentSlack++;
                        break;

                    case 1: // =
                        A[i, artificialIndex + currentArtificial] = 1;
                        isArtificial[artificialIndex + currentArtificial] = true;
                        log.AppendLine($"   Ограничение {i + 1} (=): добавлена " +
                                      $"искусственная переменная a{currentArtificial + 1}");
                        currentArtificial++;
                        break;

                    case 2: // >=
                        A[i, slackIndex + currentSlack] = -1;
                        log.AppendLine($"   Ограничение {i + 1} (&gt;=): добавлена " +
                                      $"дополнительная переменная s{currentSlack + 1} с коэф. -1");

                        A[i, artificialIndex + currentArtificial] = 1;
                        isArtificial[artificialIndex + currentArtificial] = true;
                        log.AppendLine($"   Ограничение {i + 1} (&gt;=): добавлена " +
                                      $"искусственная переменная a{currentArtificial + 1}");

                        currentSlack++;
                        currentArtificial++;
                        break;
                }
            }

            return (A, b, c, isArtificial, nVariables, slackVars, artificialVars, varNames);
        }

        private SimplexSolution BuildInitialTable(
            (double[,] A, double[] b, double[] c, bool[] isArtificial,
             int nVars, int nSlack, int nArtificial, string[] varNames) canonicalForm,
            LinearProgrammingProblem problem)
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
                TableName = "Начальная симплекс-таблица",
                ColumnHeaders = canonicalForm.varNames.ToList()
            };

            // Определяем начальный базис
            int[] basis = new int[m];
            double[] cB = new double[m];
            string[] basisNames = new string[m];

            // Заполняем базис дополнительными и искусственными переменными
            int slackIdx = 0;
            int artIdx = 0;

            for (int i = 0; i < m; i++)
            {
                var constraint = problem.Constraints[i];

                if (constraint.TypeIndex == 0) // <=
                {
                    // Для <= используем дополнительную переменную
                    basis[i] = canonicalForm.nVars + slackIdx;
                    cB[i] = 0; // Дополнительные переменные имеют Cб = 0
                    basisNames[i] = $"s{slackIdx + 1}";
                    slackIdx++;
                }
                else if (constraint.TypeIndex == 1) // =
                {
                    // Для = используем искусственную переменную
                    basis[i] = canonicalForm.nVars + canonicalForm.nSlack + artIdx;
                    cB[i] = problem.IsMaximization ? -1e6 : 1e6; // Большое M
                    basisNames[i] = $"a{artIdx + 1}";
                    artIdx++;
                }
                else if (constraint.TypeIndex == 2) // >=
                {
                    // Для >= используем дополнительную (-1) и искусственную (+1)
                    // Базисной будет искусственная переменная
                    basis[i] = canonicalForm.nVars + canonicalForm.nSlack + artIdx;
                    cB[i] = problem.IsMaximization ? -1e6 : 1e6; // Большое M
                    basisNames[i] = $"a{artIdx + 1}";
                    slackIdx++;
                    artIdx++;
                }
            }

            log.AppendLine($"   Базисные переменные:");
            for (int i = 0; i < m; i++)
            {
                log.AppendLine($"     {basisNames[i]} (строка {i + 1}) с Cб = {cB[i]:F2}");
            }

            // Заполняем строки таблицы
            for (int i = 0; i < m; i++)
            {
                var row = new SimplexRow
                {
                    BasisVariable = basisNames[i],
                    CB = cB[i],
                    RightHandSide = canonicalForm.b[i]
                };

                // Заполняем коэффициенты из матрицы A
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
            // В симплекс-таблице для максимизации показываем -Δj
            for (int j = 0; j < n; j++)
            {
                double delta = canonicalForm.c[j];
                for (int i = 0; i < m; i++)
                {
                    delta -= cB[i] * canonicalForm.A[i, j];
                }

                // Для отображения меняем знак (чтобы показывать исходные коэффициенты)
                fRow.Coefficients.Add(-delta);
            }

            // Вычисляем значение F (со знаком для максимизации/минимизации)
            double fValue = 0;
            for (int i = 0; i < m; i++)
            {
                fValue += cB[i] * canonicalForm.b[i];
            }
            fRow.RightHandSide = problem.IsMaximization ? -fValue : fValue;

            initialTable.Rows.Add(fRow);
            solution.Tables.Add(initialTable);

            log.AppendLine($"   Размер таблицы: {m}×{n}");
            log.AppendLine($"   Начальное значение F = {fRow.RightHandSide:F2}");

            return solution;
        }

        private void PerformSimplexIterations(
            SimplexSolution solution,
            (double[,] A, double[] b, double[] c, bool[] isArtificial,
             int nVars, int nSlack, int nArtificial, string[] varNames) canonicalForm,
            LinearProgrammingProblem problem)
        {
            int iteration = 0;
            int maxIterations = 50;

            while (iteration < maxIterations)
            {
                iteration++;
                log.AppendLine($"\n   Итерация {iteration}:");

                var currentTable = solution.Tables.Last();
                int m = currentTable.Rows.Count - 1; // без F-строки
                if (m <= 0 || currentTable.Rows.Count == 0)
                {
                    log.AppendLine("   Ошибка: таблица пуста или некорректна");
                    return;
                }

                int n = currentTable.Rows[0].Coefficients.Count;

                // Получаем F-строку (последняя строка)
                var fRow = currentTable.Rows[m];

                // Проверка оптимальности
                bool isOptimal = true;
                int pivotColumn = -1;

                // Для максимизации: ищем положительные оценки в F-строке
                // (так как мы показываем -Δj)
                if (problem.IsMaximization)
                {
                    double maxDelta = double.NegativeInfinity;
                    for (int j = 0; j < n; j++)
                    {
                        // Пропускаем столбцы искусственных переменных, если они еще есть
                        if (j >= canonicalForm.nVars + canonicalForm.nSlack)
                            continue;

                        double delta = fRow.Coefficients[j];
                        if (delta > EPSILON && delta > maxDelta + EPSILON)
                        {
                            maxDelta = delta;
                            pivotColumn = j;
                            isOptimal = false;
                        }
                    }
                }
                else
                {
                    // Для минимизации: ищем отрицательные оценки
                    double minDelta = double.PositiveInfinity;
                    for (int j = 0; j < n; j++)
                    {
                        if (j >= canonicalForm.nVars + canonicalForm.nSlack)
                            continue;

                        double delta = fRow.Coefficients[j];
                        if (delta < -EPSILON && delta < minDelta - EPSILON)
                        {
                            minDelta = delta;
                            pivotColumn = j;
                            isOptimal = false;
                        }
                    }
                }

                if (isOptimal)
                {
                    log.AppendLine("   ✓ Решение оптимально!");
                    currentTable.IsOptimal = true;
                    solution.IsOptimal = true;

                    // Проверяем наличие искусственных переменных в базисе с ненулевыми значениями
                    bool hasArtificialInBasis = false;
                    for (int i = 0; i < m; i++)
                    {
                        string varName = currentTable.Rows[i].BasisVariable;
                        if (varName != null && varName.StartsWith("a") &&
                            Math.Abs(currentTable.Rows[i].RightHandSide) > EPSILON)
                        {
                            hasArtificialInBasis = true;
                            break;
                        }
                    }

                    if (hasArtificialInBasis)
                    {
                        log.AppendLine("   ⚠ В базисе остались искусственные переменные с ненулевыми значениями!");
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
                    log.AppendLine("   ✗ Не удалось найти разрешающий столбец!");
                    solution.IsFeasible = false;
                    return;
                }

                log.AppendLine($"   Разрешающий столбец: {canonicalForm.varNames[pivotColumn]} " +
                              $"(оценка = {fRow.Coefficients[pivotColumn]:F4})");

                // Находим разрешающую строку по минимальному симплексному отношению
                int pivotRow = -1;
                double minRatio = double.MaxValue;

                for (int i = 0; i < m; i++)
                {
                    double aij = currentTable.Rows[i].Coefficients[pivotColumn];
                    if (aij > EPSILON)
                    {
                        double ratio = currentTable.Rows[i].RightHandSide / aij;
                        if (ratio >= 0 && ratio < minRatio - EPSILON)
                        {
                            minRatio = ratio;
                            pivotRow = i;
                        }
                    }
                }

                if (pivotRow == -1)
                {
                    log.AppendLine("   ⚠ Нет положительных коэффициентов в разрешающем столбце!");
                    log.AppendLine("   Задача неограничена!");
                    solution.IsUnbounded = true;
                    return;
                }

                log.AppendLine($"   Разрешающая строка: строка {pivotRow + 1} " +
                              $"(θ = {minRatio:F4})");
                log.AppendLine($"   Разрешающий элемент: " +
                              $"{currentTable.Rows[pivotRow].Coefficients[pivotColumn]:F4}");

                // Создаем новую таблицу
                var newTable = new SimplexTable
                {
                    TableName = $"Симплекс-таблица {iteration + 1}",
                    ColumnHeaders = canonicalForm.varNames.ToList(),
                    PivotRow = pivotRow,
                    PivotColumn = pivotColumn
                };

                double pivotElement = currentTable.Rows[pivotRow].Coefficients[pivotColumn];

                // Заполняем новую таблицу
                for (int i = 0; i <= m; i++) // включая F-строку
                {
                    var newRow = new SimplexRow
                    {
                        BasisVariable = currentTable.Rows[i].BasisVariable,
                        CB = currentTable.Rows[i].CB,
                        RightHandSide = 0,
                        IsPivotRow = (i == pivotRow)
                    };

                    // Копируем коэффициенты
                    for (int j = 0; j < n; j++)
                    {
                        newRow.Coefficients.Add(0);
                    }

                    newTable.Rows.Add(newRow);
                }

                // Обновляем базисную переменную в разрешающей строке
                newTable.Rows[pivotRow].BasisVariable = canonicalForm.varNames[pivotColumn];
                newTable.Rows[pivotRow].CB = problem.IsMaximization ?
                    -originalObjective[pivotColumn] : originalObjective[pivotColumn];

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

                // Пересчитываем F-строку отдельно для правильного отображения
                // Вычисляем новое значение F
                double newFValue = 0;
                for (int i = 0; i < m; i++)
                {
                    newFValue += newTable.Rows[i].CB * newTable.Rows[i].RightHandSide;
                }
                newTable.Rows[m].RightHandSide = problem.IsMaximization ? -newFValue : newFValue;

                // Пересчитываем оценки в F-строке
                for (int j = 0; j < n; j++)
                {
                    double delta = canonicalForm.c[j];
                    for (int i = 0; i < m; i++)
                    {
                        delta -= newTable.Rows[i].CB * newTable.Rows[i].Coefficients[j];
                    }
                    // Меняем знак для отображения исходных коэффициентов
                    newTable.Rows[m].Coefficients[j] = -delta;
                }

                solution.Tables.Add(newTable);

                log.AppendLine($"   Новое значение F = {newTable.Rows[m].RightHandSide:F4}");

                // Добавляем симплексные отношения для следующей итерации
                for (int i = 0; i < m; i++)
                {
                    double aij = newTable.Rows[i].Coefficients[pivotColumn];
                    if (aij > EPSILON)
                    {
                        double ratio = newTable.Rows[i].RightHandSide / aij;
                        if (ratio >= 0)
                        {
                            newTable.Rows[i].Theta = ratio;
                        }
                    }
                }
            }

            if (iteration >= maxIterations)
            {
                log.AppendLine("   ⚠ Превышено максимальное количество итераций!");
            }
        }

        private void ExtractSolution(
            SimplexSolution solution,
            (double[,] A, double[] b, double[] c, bool[] isArtificial,
             int nVars, int nSlack, int nArtificial, string[] varNames) canonicalForm,
            LinearProgrammingProblem originalProblem)
        {
            if (solution == null || !solution.IsOptimal || !solution.IsFeasible)
            {
                log.AppendLine("\n4. Решение не найдено или не оптимально!");
                return;
            }

            var finalTable = solution.Tables.Last();
            int m = finalTable.Rows.Count - 1; // без F-строки

            // Извлекаем значения переменных
            double[] solutionVars = new double[canonicalForm.nVars];
            double[] slackVars = new double[canonicalForm.nSlack];

            // Инициализируем нулями
            for (int i = 0; i < canonicalForm.nVars; i++) solutionVars[i] = 0;
            for (int i = 0; i < canonicalForm.nSlack; i++) slackVars[i] = 0;

            // Заполняем значения базисных переменных
            for (int i = 0; i < m; i++)
            {
                string varName = finalTable.Rows[i].BasisVariable;
                if (varName == null) continue;

                double value = finalTable.Rows[i].RightHandSide;

                if (varName.StartsWith("x"))
                {
                    if (int.TryParse(varName.Substring(1), out int index))
                    {
                        if (index >= 1 && index <= canonicalForm.nVars)
                        {
                            solutionVars[index - 1] = Math.Max(0, value); // Отрицательные значения недопустимы
                        }
                    }
                }
                else if (varName.StartsWith("s"))
                {
                    if (int.TryParse(varName.Substring(1), out int index))
                    {
                        if (index >= 1 && index <= canonicalForm.nSlack)
                        {
                            slackVars[index - 1] = Math.Max(0, value);
                        }
                    }
                }
            }

            solution.Solution = solutionVars;
            solution.SlackVariables = slackVars;

            // Вычисляем оптимальное значение целевой функции
            double optimalValue = 0;
            if (originalObjective != null)
            {
                for (int i = 0; i < Math.Min(originalObjective.Count, solutionVars.Length); i++)
                {
                    optimalValue += originalObjective[i] * solutionVars[i];
                }
            }
            solution.OptimalValue = optimalValue;

            log.AppendLine("\n4. Извлечение решения:");
            log.AppendLine($"   Оптимальное значение: F = {optimalValue:F2}");

            log.AppendLine($"\n   Значения основных переменных:");
            for (int i = 0; i < canonicalForm.nVars; i++)
            {
                log.AppendLine($"     x{i + 1} = {solutionVars[i]:F2}");
            }

            if (canonicalForm.nSlack > 0)
            {
                log.AppendLine($"\n   Значения дополнительных переменных:");
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

                for (int j = 0; j < Math.Min(canonicalForm.nVars, constraint.Coefficients.Count); j++)
                {
                    used += solutionVars[j] * constraint.Coefficients[j].Value;
                }

                double remaining = constraint.RightHandSide - used;

                log.AppendLine($"\n   {constraint.Name}:");
                log.AppendLine($"     Использовано: {used:F2} из {constraint.RightHandSide:F2}");
                log.AppendLine($"     Осталось: {Math.Max(remaining, 0):F2}");

                if (Math.Abs(remaining) < EPSILON)
                {
                    log.AppendLine($"     ✓ Ресурс использован полностью");
                }
                else if (remaining > 0)
                {
                    log.AppendLine($"     ⚠ Ресурс использован не полностью");
                }
                else
                {
                    log.AppendLine($"     ✗ Превышение ресурса на {Math.Abs(remaining):F2}");
                }
            }
        }
    }
}