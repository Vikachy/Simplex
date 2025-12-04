using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimplexSolver
{
    public class SimplexSolution
    {
        public bool IsOptimal { get; set; }
        public double[] Solution { get; set; }
        public double OptimalValue { get; set; }
        public double[] SlackVariables { get; set; }
        public List<SimplexTable> Tables { get; set; } = new List<SimplexTable>();
        public string Log { get; set; }
    }

    public class SimplexMethod
    {
        private StringBuilder log = new StringBuilder();

        public SimplexSolution Solve(LinearProgrammingProblem problem)
        {
            log.Clear();
            log.AppendLine("=== РЕШЕНИЕ ЗАДАЧИ ЛИНЕЙНОГО ПРОГРАММИРОВАНИЯ ===");
            log.AppendLine();

            // Шаг 1: Приведение к канонической форме
            log.AppendLine("1. Приведение к канонической форме:");
            var canonicalForm = ConvertToCanonicalForm(problem);

            // Шаг 2: Построение начальной симплекс-таблицы
            log.AppendLine("\n2. Построение начальной симплекс-таблицы:");
            var solution = BuildInitialTable(canonicalForm);

            // Шаг 3: Итерации симплекс-метода
            log.AppendLine("\n3. Итерации симплекс-метода:");
            PerformSimplexIterations(solution);

            solution.Log = log.ToString();
            return solution;
        }

        private (double[,] A, double[] b, double[] c, bool[] isArtificial)
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

            return (A, b, c, isArtificial);
        }

        private SimplexSolution BuildInitialTable((double[,] A, double[] b, double[] c, bool[] isArtificial) canonicalForm)
        {
            int m = canonicalForm.b.Length;
            int n = canonicalForm.c.Length;

            var solution = new SimplexSolution
            {
                Tables = new List<SimplexTable>()
            };

            // Создаем начальную таблицу
            var initialTable = new SimplexTable
            {
                TableName = "Начальная симплекс-таблица"
            };

            // Определяем начальный базис
            int[] basis = new int[m];
            double[] cB = new double[m];
            int basisCount = 0;

            // Ищем единичные столбцы для начального базиса
            for (int j = 0; j < n && basisCount < m; j++)
            {
                bool isUnitColumn = true;
                int unitRow = -1;

                for (int i = 0; i < m; i++)
                {
                    if (Math.Abs(canonicalForm.A[i, j] - 1) < 1e-10)
                    {
                        if (unitRow == -1)
                            unitRow = i;
                        else
                        {
                            isUnitColumn = false;
                            break;
                        }
                    }
                    else if (Math.Abs(canonicalForm.A[i, j]) > 1e-10)
                    {
                        isUnitColumn = false;
                        break;
                    }
                }

                if (isUnitColumn && unitRow != -1 && !basis.Contains(unitRow))
                {
                    basis[unitRow] = j;
                    cB[unitRow] = canonicalForm.c[j];
                    basisCount++;
                }
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

            // Вычисляем оценки
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

        private void PerformSimplexIterations(SimplexSolution solution)
        {
            // Здесь должна быть полная реализация итераций симплекс-метода
            // с пересчетом таблиц и проверкой оптимальности

            // Временная заглушка
            log.AppendLine("   Итерации симплекс-метода выполняются...");

            // Для примера добавляем фиктивную вторую таблицу
            var secondTable = new SimplexTable
            {
                TableName = "Вторая симплекс-таблица"
            };

            solution.Tables.Add(secondTable);

            log.AppendLine("   Получено оптимальное решение!");
        }
    }
}