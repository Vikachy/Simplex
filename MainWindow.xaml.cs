using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
                spTables.Children.Clear();
                spSolutionContainer.Children.Clear();

                // Показываем симплекс-таблицы с улучшенным отображением
                DisplaySimplexTables();

                // Показываем решение
                DisplaySolution();

                // Показываем лог
                txtLog.Text = currentSolution.Log;

                if (currentSolution.IsOptimal && currentSolution.IsFeasible)
                {
                    MessageBox.Show($"✅ Решение найдено!\n\n" +
                                  $"Оптимальное значение: {currentSolution.OptimalValue:F2}\n" +
                                  $"Количество итераций: {currentSolution.Tables.Count - 1}\n" +
                                  $"Статус: Оптимальное решение",
                                  "Решение готово",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);
                }
                else if (currentSolution.IsUnbounded)
                {
                    MessageBox.Show($"⚠ Внимание!\n\n" +
                                  $"Задача неограничена!\n" +
                                  $"Целевая функция может принимать бесконечно большое значение.",
                                  "Результат",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show($"❌ Решение не найдено!\n\n" +
                                  $"Задача не имеет допустимого решения.",
                                  "Результат",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при решении: {ex.Message}\n\nStackTrace:\n{ex.StackTrace}",
                              "Ошибка",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }

        private void DisplaySimplexTables()
        {
            if (currentSolution?.Tables == null) return;

            foreach (var table in currentSolution.Tables)
            {
                var tableControl = CreateTableControl(table);
                spTables.Children.Add(tableControl);
            }
        }

        private FrameworkElement CreateTableControl(SimplexTable table)
        {
            var mainStack = new StackPanel();
            mainStack.Margin = new Thickness(0, 0, 0, 30);
            mainStack.Background = Brushes.White;

            // Заголовок таблицы
            var title = new TextBlock
            {
                Text = table.TableName,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.DarkBlue,
                Margin = new Thickness(0, 0, 0, 15),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            mainStack.Children.Add(title);

            // Создаем Grid для таблицы
            var dataGrid = new Grid();
            dataGrid.Background = Brushes.White;
            dataGrid.Margin = new Thickness(0, 0, 0, 10);

            // Определяем количество столбцов
            int columnCount = 0;

            // Базис (70px)
            dataGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
            columnCount++;

            // Cб (60px) - НЕ показываем для F-строки
            dataGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
            columnCount++;

            // Коэффициенты переменных (по 60px каждая)
            if (table.Rows.Count > 0 && table.Rows[0].Coefficients.Count > 0)
            {
                for (int i = 0; i < table.Rows[0].Coefficients.Count; i++)
                {
                    dataGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
                    columnCount++;
                }
            }

            // Свободный член (80px)
            dataGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            columnCount++;

            // θ (80px)
            dataGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            columnCount++;

            // Создаем строки
            int currentRow = 0;

            // Заголовки столбцов
            AddCell(dataGrid, currentRow, 0, "Базис", Brushes.LightGray, FontWeights.Bold, HorizontalAlignment.Center);
            AddCell(dataGrid, currentRow, 1, "Cб", Brushes.LightGray, FontWeights.Bold, HorizontalAlignment.Center);

            // Заголовки переменных
            if (table.ColumnHeaders != null)
            {
                for (int i = 0; i < table.ColumnHeaders.Count; i++)
                {
                    AddCell(dataGrid, currentRow, i + 2, table.ColumnHeaders[i],
                           Brushes.LightGray, FontWeights.Bold, HorizontalAlignment.Center);
                }
            }
            else
            {
                for (int i = 0; i < table.Rows[0].Coefficients.Count; i++)
                {
                    AddCell(dataGrid, currentRow, i + 2, $"x{i + 1}",
                           Brushes.LightGray, FontWeights.Bold, HorizontalAlignment.Center);
                }
            }

            AddCell(dataGrid, currentRow, table.Rows[0].Coefficients.Count + 2, "Св.член",
                   Brushes.LightGray, FontWeights.Bold, HorizontalAlignment.Center);
            AddCell(dataGrid, currentRow, table.Rows[0].Coefficients.Count + 3, "θ",
                   Brushes.LightGray, FontWeights.Bold, HorizontalAlignment.Center);

            currentRow++;

            // Данные строк
            for (int i = 0; i < table.Rows.Count; i++)
            {
                var simplexRow = table.Rows[i];
                bool isFrow = simplexRow.BasisVariable == "F";

                // Базисная переменная
                Brush bgColor = isFrow ? Brushes.AliceBlue :
                               (i == table.PivotRow ? Brushes.LightGoldenrodYellow : Brushes.White);

                AddCell(dataGrid, currentRow, 0, simplexRow.BasisVariable, bgColor,
                       isFrow ? FontWeights.Bold : FontWeights.Normal, HorizontalAlignment.Center);

                // Cб - для F-строки не показываем
                if (!isFrow)
                {
                    AddCell(dataGrid, currentRow, 1, simplexRow.CB.ToString("F2"), bgColor,
                           FontWeights.Normal, HorizontalAlignment.Center);
                }
                else
                {
                    AddCell(dataGrid, currentRow, 1, "", bgColor,
                           FontWeights.Normal, HorizontalAlignment.Center);
                }

                // Коэффициенты
                for (int j = 0; j < simplexRow.Coefficients.Count; j++)
                {
                    bool isPivotCell = (i == table.PivotRow && j == table.PivotColumn);
                    Brush cellBg = isPivotCell ? Brushes.Orange : bgColor;
                    FontWeight fontWeight = isPivotCell ? FontWeights.Bold :
                                           (isFrow ? FontWeights.SemiBold : FontWeights.Normal);

                    double value = simplexRow.Coefficients[j];
                    AddCell(dataGrid, currentRow, j + 2, value.ToString("F2"), cellBg,
                           fontWeight, HorizontalAlignment.Center);
                }

                // Свободный член
                AddCell(dataGrid, currentRow, simplexRow.Coefficients.Count + 2,
                       simplexRow.RightHandSide.ToString("F2"), bgColor,
                       isFrow ? FontWeights.Bold : FontWeights.Normal, HorizontalAlignment.Center);

                // θ (только для обычных строк, не для F)
                if (!isFrow)
                {
                    string thetaText = simplexRow.Theta.HasValue ? simplexRow.Theta.Value.ToString("F2") : "-";
                    AddCell(dataGrid, currentRow, simplexRow.Coefficients.Count + 3,
                           thetaText, bgColor, FontWeights.Normal, HorizontalAlignment.Center);
                }
                else
                {
                    AddCell(dataGrid, currentRow, simplexRow.Coefficients.Count + 3,
                           "", bgColor, FontWeights.Normal, HorizontalAlignment.Center);
                }

                currentRow++;
            }

            mainStack.Children.Add(dataGrid);

            // Статус таблицы
            var statusText = new TextBlock
            {
                Text = table.IsOptimal ? "✓ Таблица оптимальна" : "🔄 Итерация продолжается",
                FontWeight = FontWeights.Bold,
                Foreground = table.IsOptimal ? Brushes.DarkGreen : Brushes.DarkOrange,
                Margin = new Thickness(0, 10, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            mainStack.Children.Add(statusText);

            return mainStack;
        }

        private void AddCell(Grid grid, int row, int column, string text, Brush background,
                           FontWeight fontWeight, HorizontalAlignment horizontalAlignment)
        {
            var border = new Border
            {
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(0.5),
                Background = background,
                Child = new TextBlock
                {
                    Text = text,
                    Padding = new Thickness(4),
                    FontWeight = fontWeight,
                    HorizontalAlignment = horizontalAlignment,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center
                }
            };

            Grid.SetRow(border, row);
            Grid.SetColumn(border, column);
            grid.Children.Add(border);
        }

        private void DisplaySolution()
        {
            spSolutionContainer.Children.Clear();

            if (currentSolution == null)
                return;

            if (!currentSolution.IsFeasible)
            {
                AddSolutionText("❌ Задача не имеет допустимого решения!",
                               Brushes.Red, FontWeights.Bold, 16);
                return;
            }

            if (currentSolution.IsUnbounded)
            {
                AddSolutionText("⚠ Задача неограничена!",
                               Brushes.Orange, FontWeights.Bold, 16);
                AddSolutionText("Целевая функция может принимать бесконечно большое значение.",
                               Brushes.Black, FontWeights.Normal, 14);
                return;
            }

            if (currentSolution.IsOptimal)
            {
                // Заголовок
                AddSolutionText("✅ ОПТИМАЛЬНОЕ РЕШЕНИЕ НАЙДЕНО",
                               Brushes.DarkGreen, FontWeights.Bold, 18);

                AddEmptyLine();

                // Целевая функция
                AddSolutionText("Оптимальное значение целевой функции:",
                               Brushes.Black, FontWeights.Normal, 14);
                AddSolutionText($"F* = {currentSolution.OptimalValue:F2}",
                               Brushes.DarkGreen, FontWeights.Bold, 18);

                AddEmptyLine();

                // Основные переменные
                AddSolutionText("Значения основных переменных:",
                               Brushes.Black, FontWeights.Normal, 14);

                if (currentSolution.Solution != null)
                {
                    for (int i = 0; i < currentSolution.Solution.Length; i++)
                    {
                        AddSolutionText($"  x{i + 1}* = {currentSolution.Solution[i]:F2}",
                                       Brushes.DarkBlue, FontWeights.SemiBold, 14, 20);
                    }
                }

                AddEmptyLine();

                // Дополнительные переменные
                if (currentSolution.SlackVariables != null && currentSolution.SlackVariables.Length > 0)
                {
                    AddSolutionText("Значения дополнительных переменных:",
                                   Brushes.Black, FontWeights.Normal, 14);

                    for (int i = 0; i < currentSolution.SlackVariables.Length; i++)
                    {
                        AddSolutionText($"  s{i + 1} = {currentSolution.SlackVariables[i]:F2}",
                                       Brushes.Gray, FontWeights.Normal, 14, 20);
                    }

                    AddEmptyLine();
                }

                // Анализ ресурсов
                AddSolutionText("📊 АНАЛИЗ ИСПОЛЬЗОВАНИЯ РЕСУРСОВ:",
                               Brushes.DarkSlateBlue, FontWeights.Bold, 15);

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
                        $"⚠ Осталось: {remaining:F2}";

                    Brush statusColor = Math.Abs(remaining) < 0.001 ?
                        Brushes.DarkGreen : Brushes.DarkOrange;

                    AddSolutionText($"{constraint.Name}:",
                                   Brushes.Black, FontWeights.SemiBold, 13, 10);
                    AddSolutionText($"  Использовано: {used:F2} из {constraint.RightHandSide:F2}",
                                   Brushes.Gray, FontWeights.Normal, 12, 25);
                    AddSolutionText($"  {status}",
                                   statusColor, FontWeights.SemiBold, 12, 25);
                    AddEmptyLine(5);
                }
            }
        }

        private void AddSolutionText(string text, Brush color, FontWeight fontWeight,
                                   double fontSize, double marginLeft = 0)
        {
            var textBlock = new TextBlock
            {
                Text = text,
                Foreground = color,
                FontWeight = fontWeight,
                FontSize = fontSize,
                Margin = new Thickness(marginLeft, 5, 0, 5),
                TextWrapping = TextWrapping.Wrap
            };
            spSolutionContainer.Children.Add(textBlock);
        }

        private void AddEmptyLine(double height = 10)
        {
            spSolutionContainer.Children.Add(new Border
            {
                Height = height,
                Background = Brushes.Transparent
            });
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            currentProblem.ObjectiveCoefficients.Clear();
            currentProblem.Constraints.Clear();
            spTables.Children.Clear();
            txtLog.Text = "";
            spSolutionContainer.Children.Clear();
            currentSolution = null;
        }

        private void ExportToExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "CSV файлы (*.csv)|*.csv|Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*",
                    FileName = "Симплекс_решение.csv",
                    DefaultExt = ".csv"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    ExportToExcelCSV(saveDialog.FileName);
                    MessageBox.Show("Результаты успешно экспортированы в файл!\n" +
                                  "Файл можно открыть в Excel как CSV.",
                                  "Экспорт завершен",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте: {ex.Message}",
                              "Ошибка",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }

        private void ExportToExcelCSV(string filePath)
        {
            using (StreamWriter writer = new StreamWriter(filePath, false, System.Text.Encoding.GetEncoding(1251)))
            {
                // Используем разделитель-запятую для Excel
                var separator = ",";

                // 1. Заголовок документа
                writer.WriteLine($"СИМПЛЕКС-МЕТОД: РЕШЕНИЕ ЗАДАЧИ ЛИНЕЙНОГО ПРОГРАММИРОВАНИЯ{separator}");
                writer.WriteLine($"{separator}");

                // 2. Основная информация о задаче
                writer.WriteLine($"ДАТА РЕШЕНИЯ{separator}{DateTime.Now:dd.MM.yyyy HH:mm:ss}");
                writer.WriteLine($"ТИП ОПТИМИЗАЦИИ{separator}{(currentProblem.IsMaximization ? "Максимизация" : "Минимизация")}");
                writer.WriteLine($"КОЛИЧЕСТВО ПЕРЕМЕННЫХ{separator}{currentProblem.ObjectiveCoefficients.Count}");
                writer.WriteLine($"КОЛИЧЕСТВО ОГРАНИЧЕНИЙ{separator}{currentProblem.Constraints.Count}");
                writer.WriteLine($"{separator}");

                // 3. Постановка задачи
                writer.WriteLine($"ПОСТАНОВКА ЗАДАЧИ{separator}");
                writer.WriteLine($"{separator}");

                // Целевая функция
                string objective = "F(x) = ";
                for (int i = 0; i < currentProblem.ObjectiveCoefficients.Count; i++)
                {
                    objective += $"{currentProblem.ObjectiveCoefficients[i].Value:F2} * x{i + 1}";
                    if (i < currentProblem.ObjectiveCoefficients.Count - 1)
                        objective += " + ";
                }
                objective += $" → {(currentProblem.IsMaximization ? "max" : "min")}";
                writer.WriteLine($"Целевая функция{separator}{objective}");
                writer.WriteLine($"{separator}");

                // Коэффициенты целевой функции в табличном виде
                writer.WriteLine($"КОЭФФИЦИЕНТЫ ЦЕЛЕВОЙ ФУНКЦИИ{separator}");
                writer.Write($"Переменная{separator}");
                for (int i = 0; i < currentProblem.ObjectiveCoefficients.Count; i++)
                {
                    writer.Write($"x{i + 1}");
                    if (i < currentProblem.ObjectiveCoefficients.Count - 1)
                        writer.Write(separator);
                }
                writer.WriteLine();

                writer.Write($"Коэффициент{separator}");
                for (int i = 0; i < currentProblem.ObjectiveCoefficients.Count; i++)
                {
                    writer.Write($"{currentProblem.ObjectiveCoefficients[i].Value:F2}");
                    if (i < currentProblem.ObjectiveCoefficients.Count - 1)
                        writer.Write(separator);
                }
                writer.WriteLine();
                writer.WriteLine($"{separator}");

                // 4. Ограничения
                writer.WriteLine($"СИСТЕМА ОГРАНИЧЕНИЙ{separator}");
                writer.WriteLine($"№{separator}Наименование{separator}Тип{separator}Выражение{separator}Правая часть{separator}");

                for (int i = 0; i < currentProblem.Constraints.Count; i++)
                {
                    var constraint = currentProblem.Constraints[i];

                    // Формируем выражение
                    string expression = "";
                    bool first = true;
                    for (int j = 0; j < constraint.Coefficients.Count; j++)
                    {
                        double coeffValue = constraint.Coefficients[j].Value;
                        if (Math.Abs(coeffValue) > 0.001)
                        {
                            if (!first) expression += " + ";
                            expression += $"{coeffValue:F2} * x{j + 1}";
                            first = false;
                        }
                    }

                    if (string.IsNullOrEmpty(expression))
                        expression = "0";

                    string sign = constraint.TypeIndex == 0 ? "≤" :
                                 constraint.TypeIndex == 1 ? "=" : "≥";

                    writer.WriteLine($"{i + 1}{separator}{constraint.Name}{separator}{sign}{separator}{expression}{separator}{constraint.RightHandSide:F2}");
                }
                writer.WriteLine($"{separator}");

                // 5. Результаты решения
                writer.WriteLine($"РЕЗУЛЬТАТЫ РЕШЕНИЯ{separator}");
                writer.WriteLine($"{separator}");

                if (currentSolution != null)
                {
                    // Статус решения
                    string status = "Не определен";
                    if (currentSolution.IsOptimal && currentSolution.IsFeasible)
                        status = "Оптимальное решение найдено";
                    else if (currentSolution.IsUnbounded)
                        status = "Задача неограничена";
                    else if (!currentSolution.IsFeasible)
                        status = "Задача не имеет допустимого решения";

                    writer.WriteLine($"СТАТУС РЕШЕНИЯ{separator}{status}");
                    writer.WriteLine($"КОЛИЧЕСТВО ИТЕРАЦИЙ{separator}{currentSolution.Tables.Count - 1}");
                    writer.WriteLine($"{separator}");

                    if (currentSolution.IsOptimal && currentSolution.IsFeasible)
                    {
                        // Оптимальное значение
                        writer.WriteLine($"ОПТИМАЛЬНОЕ ЗНАЧЕНИЕ{separator}{currentSolution.OptimalValue:F2}");
                        writer.WriteLine($"{separator}");

                        // Значения переменных
                        writer.WriteLine($"ОПТИМАЛЬНЫЕ ЗНАЧЕНИЯ ПЕРЕМЕННЫХ{separator}");
                        writer.WriteLine($"Переменная{separator}Значение{separator}Статус{separator}");

                        for (int i = 0; i < currentSolution.Solution.Length; i++)
                        {
                            writer.WriteLine($"x{i + 1}{separator}{currentSolution.Solution[i]:F2}{separator}{(Math.Abs(currentSolution.Solution[i]) > 0.001 ? "Базисная" : "Нулевая")}");
                        }
                        writer.WriteLine($"{separator}");

                        // Дополнительные переменные
                        if (currentSolution.SlackVariables != null && currentSolution.SlackVariables.Length > 0)
                        {
                            writer.WriteLine($"ДОПОЛНИТЕЛЬНЫЕ ПЕРЕМЕННЫЕ{separator}");
                            writer.WriteLine($"Переменная{separator}Значение{separator}Статус{separator}");

                            for (int i = 0; i < currentSolution.SlackVariables.Length; i++)
                            {
                                string slackStatus = Math.Abs(currentSolution.SlackVariables[i]) < 0.001 ?
                                                   "Ресурс использован полностью" :
                                                   $"Остаток ресурса: {currentSolution.SlackVariables[i]:F2}";
                                writer.WriteLine($"s{i + 1}{separator}{currentSolution.SlackVariables[i]:F2}{separator}{slackStatus}");
                            }
                            writer.WriteLine($"{separator}");
                        }

                        // Анализ использования ресурсов
                        writer.WriteLine($"АНАЛИЗ ИСПОЛЬЗОВАНИЯ РЕСУРСОВ{separator}");
                        writer.WriteLine($"Ресурс{separator}Использовано{separator}Доступно{separator}Остаток{separator}Статус{separator}");

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
                            string resourceStatus = Math.Abs(remaining) < 0.001 ?
                                                  "Использован полностью" :
                                                  "Использован частично";

                            writer.WriteLine($"{constraint.Name}{separator}{used:F2}{separator}{constraint.RightHandSide:F2}{separator}{Math.Max(remaining, 0):F2}{separator}{resourceStatus}");
                        }
                        writer.WriteLine($"{separator}");

                        // 6. Симплекс-таблицы
                        writer.WriteLine($"СИМПЛЕКС-ТАБЛИЦЫ (Всего: {currentSolution.Tables.Count}){separator}");
                        writer.WriteLine($"{separator}");

                        foreach (var table in currentSolution.Tables)
                        {
                            writer.WriteLine($"{table.TableName.ToUpper()}{separator}");

                            // Заголовки таблицы
                            writer.Write($"Базис{separator}Cб{separator}");
                            if (table.ColumnHeaders != null)
                            {
                                for (int i = 0; i < table.ColumnHeaders.Count; i++)
                                {
                                    writer.Write(table.ColumnHeaders[i]);
                                    if (i < table.ColumnHeaders.Count - 1)
                                        writer.Write(separator);
                                }
                            }
                            writer.Write($"{separator}Св.член{separator}θ");
                            writer.WriteLine();

                            // Данные таблицы
                            foreach (var row in table.Rows)
                            {
                                // Базисная переменная
                                writer.Write($"{row.BasisVariable}{separator}");

                                // Cб (для F-строки пусто)
                                if (row.BasisVariable == "F")
                                    writer.Write($"{separator}");
                                else
                                    writer.Write($"{row.CB:F2}{separator}");

                                // Коэффициенты
                                for (int i = 0; i < row.Coefficients.Count; i++)
                                {
                                    writer.Write($"{row.Coefficients[i]:F2}");
                                    if (i < row.Coefficients.Count - 1)
                                        writer.Write(separator);
                                }

                                // Свободный член и θ
                                writer.Write($"{separator}{row.RightHandSide:F2}{separator}");
                                writer.WriteLine(row.Theta.HasValue ? row.Theta.Value.ToString("F2") : "-");
                            }

                            // Статус таблицы
                            string tableStatus = table.IsOptimal ? "ОПТИМАЛЬНАЯ ТАБЛИЦА" : "ПРОМЕЖУТОЧНАЯ ТАБЛИЦА";
                            writer.WriteLine($"СТАТУС{separator}{tableStatus}");
                            writer.WriteLine($"{separator}");
                        }

                        // 7. Ключевые разрешающие элементы
                        writer.WriteLine($"КЛЮЧЕВЫЕ РАЗРЕШАЮЩИЕ ЭЛЕМЕНТЫ{separator}");
                        writer.WriteLine($"Итерация{separator}Разрешающая строка{separator}Разрешающий столбец{separator}Элемент{separator}");

                        for (int i = 1; i < currentSolution.Tables.Count; i++) // Пропускаем начальную таблицу
                        {
                            var table = currentSolution.Tables[i];
                            if (table.PivotRow >= 0 && table.PivotColumn >= 0 && table.Rows.Count > table.PivotRow)
                            {
                                var pivotRowData = table.Rows[table.PivotRow];
                                if (table.PivotColumn < pivotRowData.Coefficients.Count)
                                {
                                    double pivotValue = pivotRowData.Coefficients[table.PivotColumn];
                                    writer.WriteLine($"{i}{separator}Строка {table.PivotRow + 1}{separator}Столбец {table.ColumnHeaders[table.PivotColumn]}{separator}{pivotValue:F4}");
                                }
                            }
                        }
                        writer.WriteLine($"{separator}");
                    }

                    // 8. Лог вычислений (основные этапы)
                    writer.WriteLine($"ОСНОВНЫЕ ЭТАПЫ РЕШЕНИЯ{separator}");
                    writer.WriteLine($"{separator}");

                    if (!string.IsNullOrEmpty(currentSolution.Log))
                    {
                        string[] logLines = currentSolution.Log.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var line in logLines)
                        {
                            if (line.Contains("===") || line.Contains("✓") || line.Contains("⚠") || line.Contains("✗") ||
                                line.Contains("Ресурс") || line.Contains("Оптимальное") || line.Contains("Итерация"))
                            {
                                writer.WriteLine($"{line.Trim()}{separator}");
                            }
                        }
                    }

                    // 9. Итоговая сводка
                    writer.WriteLine($"ИТОГОВАЯ СВОДКА{separator}");
                    writer.WriteLine($"{separator}");

                    writer.WriteLine($"Задача решена успешно: {(currentSolution.IsOptimal && currentSolution.IsFeasible ? "ДА" : "НЕТ")}{separator}");
                    writer.WriteLine($"Тип задачи: {(currentProblem.IsMaximization ? "Максимизация прибыли" : "Минимизация затрат")}{separator}");
                    writer.WriteLine($"Конечный результат: {(currentSolution.IsOptimal && currentSolution.IsFeasible ? $"F = {currentSolution.OptimalValue:F2}" : "Решение не найдено")}{separator}");
                    writer.WriteLine($"Время решения: {DateTime.Now:HH:mm:ss}{separator}");
                    writer.WriteLine($"{separator}");

  
                    writer.WriteLine($"ПРОВЕРОЧНЫЕ ВЫЧИСЛЕНИЯ{separator}");
                    writer.WriteLine($"{separator}");

                    if (currentSolution.IsOptimal && currentSolution.IsFeasible && currentSolution.Solution != null)
                    {
                        writer.WriteLine($"Проверка целевой функции:{separator}");
                        writer.Write($"F = ");
                        double checkF = 0;
                        for (int i = 0; i < Math.Min(currentProblem.ObjectiveCoefficients.Count, currentSolution.Solution.Length); i++)
                        {
                            double term = currentProblem.ObjectiveCoefficients[i].Value * currentSolution.Solution[i];
                            checkF += term;
                            writer.Write($"{currentProblem.ObjectiveCoefficients[i].Value:F2} × {currentSolution.Solution[i]:F2}");
                            if (i < Math.Min(currentProblem.ObjectiveCoefficients.Count, currentSolution.Solution.Length) - 1)
                                writer.Write(" + ");
                        }
                        writer.WriteLine($" = {checkF:F2}{separator}");

                        // Исправленная строка с тернарным оператором в скобках:
                        writer.WriteLine($"Совпадение с расчетным: {(Math.Abs(checkF - currentSolution.OptimalValue) < 0.01 ? "ДА" : "НЕТ (расхождение)")}{separator}");
                        writer.WriteLine($"{separator}");

                        // Проверка ограничений
                        writer.WriteLine($"ПРОВЕРКА ОГРАНИЧЕНИЙ:{separator}");
                        for (int i = 0; i < currentProblem.Constraints.Count; i++)
                        {
                            var constraint = currentProblem.Constraints[i];
                            double leftSide = 0;

                            writer.Write($"{constraint.Name}: ");
                            for (int j = 0; j < Math.Min(constraint.Coefficients.Count, currentSolution.Solution.Length); j++)
                            {
                                leftSide += constraint.Coefficients[j].Value * currentSolution.Solution[j];
                                writer.Write($"{constraint.Coefficients[j].Value:F2} × {currentSolution.Solution[j]:F2}");
                                if (j < Math.Min(constraint.Coefficients.Count, currentSolution.Solution.Length) - 1)
                                    writer.Write(" + ");
                            }

                            writer.Write($" = {leftSide:F2}");
                            string sign = constraint.TypeIndex == 0 ? "≤" :
                                         constraint.TypeIndex == 1 ? "=" : "≥";
                            writer.WriteLine($" {sign} {constraint.RightHandSide:F2}");

                            // Проверка выполнения ограничения
                            bool constraintOK = false;
                            if (constraint.TypeIndex == 0) // ≤
                                constraintOK = leftSide <= constraint.RightHandSide + 0.001;
                            else if (constraint.TypeIndex == 1) // =
                                constraintOK = Math.Abs(leftSide - constraint.RightHandSide) < 0.001;
                            else if (constraint.TypeIndex == 2) // ≥
                                constraintOK = leftSide >= constraint.RightHandSide - 0.001;

                            // Тернарный оператор тоже нужно заключить в скобки:
                            writer.WriteLine($"Ограничение выполняется: {(constraintOK ? "ДА" : "НЕТ")}{separator}");
                        }
                    }
                }
                else
                {
                    writer.WriteLine($"РЕШЕНИЕ НЕ НАЙДЕНО{separator}Нет данных для экспорта{separator}");
                }

                // Футер документа
                writer.WriteLine($"{separator}");
                writer.WriteLine(new string('=', 80));
                writer.WriteLine($"Сгенерировано программой 'Симплекс-метод' {DateTime.Now:dd.MM.yyyy HH:mm}{separator}");
                writer.WriteLine($"Автор: Система автоматического решения задач ЛП{separator}");
            }
        }
    }
}