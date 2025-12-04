using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace SimplexSolver
{
    public partial class MainWindow : Window
    {
        private LinearProgrammingProblem currentProblem = new LinearProgrammingProblem();
        private SimplexMethod simplexSolver = new SimplexMethod();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = currentProblem;
            LoadExample14();
        }

        private void LoadExample_Click(object sender, RoutedEventArgs e)
        {
            LoadExample14();
        }

        private void LoadExample14()
        {
            // Очищаем текущую задачу
            currentProblem.ObjectiveCoefficients.Clear();
            currentProblem.Constraints.Clear();

            // Вариант 14 из задания
            // Целевая функция: max F = 6x1 + 5x2 + 7x3
            currentProblem.IsMaximization = true;
            currentProblem.ObjectiveCoefficients.Add(new Coefficient { Index = 1, Value = 6 });
            currentProblem.ObjectiveCoefficients.Add(new Coefficient { Index = 2, Value = 5 });
            currentProblem.ObjectiveCoefficients.Add(new Coefficient { Index = 3, Value = 7 });

            // Ограничения из варианта 14
            // 1) 2x1 + 3x2 + 2x3 <= 60
            var c1 = new Constraint { Name = "Оборудование А" };
            c1.Coefficients.Add(new Coefficient { Index = 1, Value = 2 });
            c1.Coefficients.Add(new Coefficient { Index = 2, Value = 3 });
            c1.Coefficients.Add(new Coefficient { Index = 3, Value = 2 });
            c1.TypeIndex = 0; // <=
            c1.RightHandSide = 60;

            // 2) 3x2 + 4x3 <= 80
            var c2 = new Constraint { Name = "Оборудование Б" };
            c2.Coefficients.Add(new Coefficient { Index = 1, Value = 0 });
            c2.Coefficients.Add(new Coefficient { Index = 2, Value = 3 });
            c2.Coefficients.Add(new Coefficient { Index = 3, Value = 4 });
            c2.TypeIndex = 0; // <=
            c2.RightHandSide = 80;

            // 3) 6x1 + x2 <= 80
            var c3 = new Constraint { Name = "Оборудование В" };
            c3.Coefficients.Add(new Coefficient { Index = 1, Value = 6 });
            c3.Coefficients.Add(new Coefficient { Index = 2, Value = 1 });
            c3.Coefficients.Add(new Coefficient { Index = 3, Value = 0 });
            c3.TypeIndex = 0; // <=
            c3.RightHandSide = 80;

            // 4) x1 + 5x2 + x3 <= 50
            var c4 = new Constraint { Name = "Оборудование Г" };
            c4.Coefficients.Add(new Coefficient { Index = 1, Value = 1 });
            c4.Coefficients.Add(new Coefficient { Index = 2, Value = 5 });
            c4.Coefficients.Add(new Coefficient { Index = 3, Value = 1 });
            c4.TypeIndex = 0; // <=
            c4.RightHandSide = 50;

            // 5) 3x1 + 4x3 <= 56
            var c5 = new Constraint { Name = "Оборудование Д" };
            c5.Coefficients.Add(new Coefficient { Index = 1, Value = 3 });
            c5.Coefficients.Add(new Coefficient { Index = 2, Value = 0 });
            c5.Coefficients.Add(new Coefficient { Index = 3, Value = 4 });
            c5.TypeIndex = 0; // <=
            c5.RightHandSide = 56;

            currentProblem.Constraints.Add(c1);
            currentProblem.Constraints.Add(c2);
            currentProblem.Constraints.Add(c3);
            currentProblem.Constraints.Add(c4);
            currentProblem.Constraints.Add(c5);

            // Обновляем интерфейс
            txtVariables.Text = "3";
            txtConstraints.Text = "5";
            cmbGoal.SelectedIndex = 0;

            MessageBox.Show("Загружен вариант 14 из задания:\n\n" +
                          "F(x) = 6x₁ + 5x₂ + 7x₃ → max\n\n" +
                          "Ограничения:\n" +
                          "2x₁ + 3x₂ + 2x₃ ≤ 60\n" +
                          "3x₂ + 4x₃ ≤ 80\n" +
                          "6x₁ + x₂ ≤ 80\n" +
                          "x₁ + 5x₂ + x₃ ≤ 50\n" +
                          "3x₁ + 4x₃ ≤ 56",
                          "Пример загружен",
                          MessageBoxButton.OK,
                          MessageBoxImage.Information);
        }

        private void CreateTable_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int nVariables = int.Parse(txtVariables.Text);
                int nConstraints = int.Parse(txtConstraints.Text);

                // Очищаем коэффициенты целевой функции
                currentProblem.ObjectiveCoefficients.Clear();
                for (int i = 0; i < nVariables; i++)
                {
                    currentProblem.ObjectiveCoefficients.Add(
                        new Coefficient { Index = i + 1, Value = 0 });
                }

                // Очищаем и создаем новые ограничения
                currentProblem.Constraints.Clear();
                for (int i = 0; i < nConstraints; i++)
                {
                    var constraint = new Constraint
                    {
                        Name = $"Ограничение {i + 1}"
                    };

                    for (int j = 0; j < nVariables; j++)
                    {
                        constraint.Coefficients.Add(
                            new Coefficient { Index = j + 1, Value = 0 });
                    }

                    constraint.TypeIndex = 0;
                    constraint.RightHandSide = 0;

                    currentProblem.Constraints.Add(constraint);
                }

                MessageBox.Show($"Создана таблица для {nVariables} переменных и {nConstraints} ограничений",
                              "Таблица создана",
                              MessageBoxButton.OK,
                              MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании таблицы: {ex.Message}",
                              "Ошибка",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }

        private void AddConstraint_Click(object sender, RoutedEventArgs e)
        {
            int nVariables = currentProblem.ObjectiveCoefficients.Count;
            if (nVariables == 0)
            {
                MessageBox.Show("Сначала создайте таблицу с переменными",
                              "Ошибка",
                              MessageBoxButton.OK,
                              MessageBoxImage.Warning);
                return;
            }

            var constraint = new Constraint
            {
                Name = $"Ограничение {currentProblem.Constraints.Count + 1}"
            };

            for (int i = 0; i < nVariables; i++)
            {
                constraint.Coefficients.Add(
                    new Coefficient { Index = i + 1, Value = 0 });
            }

            constraint.TypeIndex = 0;
            constraint.RightHandSide = 0;

            currentProblem.Constraints.Add(constraint);
        }

        private void RemoveConstraint_Click(object sender, RoutedEventArgs e)
        {
            if (currentProblem.Constraints.Count > 0)
            {
                currentProblem.Constraints.RemoveAt(currentProblem.Constraints.Count - 1);
            }
        }

        private void SolveSimplex_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверяем ввод
                if (currentProblem.ObjectiveCoefficients.Count == 0)
                {
                    MessageBox.Show("Не заданы коэффициенты целевой функции",
                                  "Ошибка",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                    return;
                }

                if (currentProblem.Constraints.Count == 0)
                {
                    MessageBox.Show("Не заданы ограничения",
                                  "Ошибка",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                    return;
                }

                // Решаем задачу
                var solution = simplexSolver.Solve(currentProblem);

                // Очищаем результаты
                icSimplexTables.ItemsSource = null;
                spResults.Children.Clear();

                // Показываем симплекс-таблицы
                icSimplexTables.ItemsSource = solution.Tables;

                // Показываем решение
                if (solution.Solution != null)
                {
                    tbSolution.Text = "Оптимальное решение найдено:";

                    for (int i = 0; i < solution.Solution.Length; i++)
                    {
                        var textBlock = new TextBlock
                        {
                            Text = $"x{i + 1} = {solution.Solution[i]:F2}",
                            FontSize = 14,
                            Margin = new Thickness(20, 5, 0, 0)
                        };
                        spResults.Children.Add(textBlock);
                    }

                    tbOptimalValue.Text = $"Максимальная прибыль: {solution.OptimalValue:F2}";
                }
                else
                {
                    tbSolution.Text = "Решение не найдено или задача не имеет решения";
                }

                // Показываем лог
                txtLog.Text = solution.Log;

                MessageBox.Show("Решение найдено! Перейдите на вкладку 'Симплекс-таблицы' " +
                              "для просмотра шагов решения или 'Решение' для итогов.",
                              "Решение готово",
                              MessageBoxButton.OK,
                              MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при решении: {ex.Message}",
                              "Ошибка",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            currentProblem.ObjectiveCoefficients.Clear();
            currentProblem.Constraints.Clear();
            icSimplexTables.ItemsSource = null;
            txtLog.Text = "";
            spResults.Children.Clear();
        }

        private void ExportToExcel_Click(object sender, RoutedEventArgs e)
        {
            // Здесь будет реализация экспорта в Excel
            // Используем библиотеку EPPlus или Interop Excel

            MessageBox.Show("Экспорт в Excel будет реализован в следующей версии",
                          "В разработке",
                          MessageBoxButton.OK,
                          MessageBoxImage.Information);
        }
    }
}