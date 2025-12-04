using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.IO;

namespace SimplexSolver
{
    public partial class MainWindow : Window
    {
        private LinearProgrammingProblem currentProblem = new LinearProgrammingProblem();
        private SimplexMethod simplexSolver = new SimplexMethod();
        private SimplexSolution currentSolution;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = currentProblem;
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

                // Устанавливаем тип оптимизации
                currentProblem.IsMaximization = (cmbGoal.SelectedIndex == 0);

                // Решаем задачу
                currentSolution = simplexSolver.Solve(currentProblem);

                // Очищаем результаты
                icSimplexTables.ItemsSource = null;
                spResults.Children.Clear();

                // Показываем симплекс-таблицы
                icSimplexTables.ItemsSource = currentSolution.Tables;

                // Показываем решение
                DisplaySolution();

                // Показываем лог
                txtLog.Text = currentSolution.Log;

                if (currentSolution.IsOptimal && currentSolution.IsFeasible)
                {
                    MessageBox.Show($"Решение найдено!\n" +
                                  $"Оптимальное значение: {currentSolution.OptimalValue:F2}\n" +
                                  $"Количество итераций: {currentSolution.Tables.Count - 1}",
                                  "Решение готово",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Решение не найдено!\n" +
                                  $"Оптимальность: {currentSolution.IsOptimal}\n" +
                                  $"Допустимость: {currentSolution.IsFeasible}\n" +
                                  $"Неограниченность: {currentSolution.IsUnbounded}",
                                  "Результат",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при решении: {ex.Message}\n{ex.StackTrace}",
                              "Ошибка",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }

        private void DisplaySolution()
        {
            spResults.Children.Clear();

            if (currentSolution == null)
                return;

            if (!currentSolution.IsFeasible)
            {
                var tb = new TextBlock
                {
                    Text = "Задача не имеет допустимого решения!",
                    FontSize = 14,
                    Foreground = System.Windows.Media.Brushes.Red,
                    Margin = new Thickness(0, 0, 0, 10)
                };
                spResults.Children.Add(tb);
                return;
            }

            if (currentSolution.IsUnbounded)
            {
                var tb = new TextBlock
                {
                    Text = "Задача неограничена! Целевая функция может принимать бесконечно большое значение.",
                    FontSize = 14,
                    Foreground = System.Windows.Media.Brushes.Orange,
                    Margin = new Thickness(0, 0, 0, 10)
                };
                spResults.Children.Add(tb);
                return;
            }

            if (currentSolution.IsOptimal)
            {
                var tb1 = new TextBlock
                {
                    Text = "ОПТИМАЛЬНОЕ РЕШЕНИЕ НАЙДЕНО",
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 20)
                };
                spResults.Children.Add(tb1);

                var tb2 = new TextBlock
                {
                    Text = $"Оптимальное значение целевой функции:",
                    FontSize = 14,
                    Margin = new Thickness(0, 0, 0, 5)
                };
                spResults.Children.Add(tb2);

                var tb3 = new TextBlock
                {
                    Text = $"F* = {currentSolution.OptimalValue:F2}",
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Foreground = System.Windows.Media.Brushes.DarkGreen,
                    Margin = new Thickness(20, 0, 0, 20)
                };
                spResults.Children.Add(tb3);

                var tb4 = new TextBlock
                {
                    Text = "Значения переменных:",
                    FontSize = 14,
                    Margin = new Thickness(0, 0, 0, 10)
                };
                spResults.Children.Add(tb4);

                if (currentSolution.Solution != null)
                {
                    for (int i = 0; i < currentSolution.Solution.Length; i++)
                    {
                        var varTb = new TextBlock
                        {
                            Text = $"x{i + 1}* = {currentSolution.Solution[i]:F2}",
                            FontSize = 14,
                            Margin = new Thickness(20, 0, 0, 5)
                        };
                        spResults.Children.Add(varTb);
                    }
                }

                if (currentSolution.SlackVariables != null && currentSolution.SlackVariables.Length > 0)
                {
                    var tb5 = new TextBlock
                    {
                        Text = "\nЗначения дополнительных переменных:",
                        FontSize = 14,
                        Margin = new Thickness(0, 10, 0, 10)
                    };
                    spResults.Children.Add(tb5);

                    for (int i = 0; i < currentSolution.SlackVariables.Length; i++)
                    {
                        var slackTb = new TextBlock
                        {
                            Text = $"s{i + 1} = {currentSolution.SlackVariables[i]:F2}",
                            FontSize = 14,
                            Margin = new Thickness(20, 0, 0, 5)
                        };
                        spResults.Children.Add(slackTb);
                    }
                }

                // Анализ использования ресурсов
                var tb6 = new TextBlock
                {
                    Text = "\nАНАЛИЗ ИСПОЛЬЗОВАНИЯ РЕСУРСОВ:",
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 20, 0, 10)
                };
                spResults.Children.Add(tb6);

                for (int i = 0; i < currentProblem.Constraints.Count; i++)
                {
                    var constraint = currentProblem.Constraints[i];
                    double used = 0;

                    if (currentSolution.Solution != null)
                    {
                        for (int j = 0; j < Math.Min(currentSolution.Solution.Length,
                                                     constraint.Coefficients.Count); j++)
                        {
                            used += currentSolution.Solution[j] * constraint.Coefficients[j].Value;
                        }
                    }

                    double remaining = constraint.RightHandSide - used;
                    string status = Math.Abs(remaining) < 0.001 ?
                        "✓ Использован полностью" :
                        $"Осталось: {remaining:F2}";

                    var resourceTb = new TextBlock
                    {
                        Text = $"{constraint.Name}: {used:F2} из {constraint.RightHandSide:F2} - {status}",
                        FontSize = 13,
                        Margin = new Thickness(10, 0, 0, 5),
                        Foreground = Math.Abs(remaining) < 0.001 ?
                            System.Windows.Media.Brushes.DarkGreen :
                            System.Windows.Media.Brushes.Black
                    };
                    spResults.Children.Add(resourceTb);
                }
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            currentProblem.ObjectiveCoefficients.Clear();
            currentProblem.Constraints.Clear();
            icSimplexTables.ItemsSource = null;
            txtLog.Text = "";
            spResults.Children.Clear();
            currentSolution = null;
        }

        private void ExportToExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*",
                    FileName = "Симплекс_решение.xlsx",
                    DefaultExt = ".xlsx"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    ExportToExcel(saveDialog.FileName);
                    MessageBox.Show("Результаты успешно экспортированы в Excel!",
                                  "Экспорт завершен",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте в Excel: {ex.Message}",
                              "Ошибка",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }

        private void ExportToExcel(string filePath)
        {
            // Простая реализация экспорта в CSV (можно заменить на EPPlus)
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                // Заголовок
                writer.WriteLine("Симплекс-метод: Решение задачи линейного программирования");
                writer.WriteLine();

                // Постановка задачи
                writer.WriteLine("ПОСТАНОВКА ЗАДАЧИ:");
                writer.Write("F(x) = ");
                for (int i = 0; i < currentProblem.ObjectiveCoefficients.Count; i++)
                {
                    writer.Write($"{currentProblem.ObjectiveCoefficients[i].Value:F2} * x{i + 1}");
                    if (i < currentProblem.ObjectiveCoefficients.Count - 1)
                        writer.Write(" + ");
                }
                writer.WriteLine($" → {(currentProblem.IsMaximization ? "max" : "min")}");
                writer.WriteLine();

                writer.WriteLine("Ограничения:");
                for (int i = 0; i < currentProblem.Constraints.Count; i++)
                {
                    var constraint = currentProblem.Constraints[i];
                    writer.Write($"{constraint.Name}: ");

                    for (int j = 0; j < constraint.Coefficients.Count; j++)
                    {
                        if (Math.Abs(constraint.Coefficients[j].Value) > 0.001)
                        {
                            writer.Write($"{constraint.Coefficients[j].Value:F2} * x{j + 1}");
                            if (j < constraint.Coefficients.Count - 1)
                                writer.Write(" + ");
                        }
                    }

                    string sign = constraint.TypeIndex == 0 ? "<=" :
                                 constraint.TypeIndex == 1 ? "=" : ">=";
                    writer.WriteLine($" {sign} {constraint.RightHandSide:F2}");
                }
                writer.WriteLine();

                // Результаты
                if (currentSolution != null)
                {
                    writer.WriteLine("РЕЗУЛЬТАТЫ:");
                    if (currentSolution.IsOptimal && currentSolution.IsFeasible)
                    {
                        writer.WriteLine($"Оптимальное значение: F* = {currentSolution.OptimalValue:F2}");
                        writer.WriteLine("Значения переменных:");
                        for (int i = 0; i < currentSolution.Solution.Length; i++)
                        {
                            writer.WriteLine($"  x{i + 1}* = {currentSolution.Solution[i]:F2}");
                        }
                    }
                    else
                    {
                        writer.WriteLine("Решение не найдено или задача не имеет допустимого решения.");
                    }
                    writer.WriteLine();

                    // Лог вычислений
                    writer.WriteLine("ЛОГ ВЫЧИСЛЕНИЙ:");
                    writer.WriteLine(currentSolution.Log);
                }
            }
        }
    }
}