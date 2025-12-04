using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace SimplexSolver
{
    public class Coefficient : INotifyPropertyChanged
    {
        private double _value;
        public double Value
        {
            get => _value;
            set
            {
                _value = value;
                OnPropertyChanged(nameof(Value));
            }
        }

        public int Index { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class Constraint : INotifyPropertyChanged
    {
        public string Name { get; set; }

        private int _typeIndex;
        public int TypeIndex
        {
            get => _typeIndex;
            set
            {
                _typeIndex = value;
                OnPropertyChanged(nameof(TypeIndex));
            }
        }

        public ObservableCollection<Coefficient> Coefficients { get; set; } = new ObservableCollection<Coefficient>();

        private double _rightHandSide;
        public double RightHandSide
        {
            get => _rightHandSide;
            set
            {
                _rightHandSide = value;
                OnPropertyChanged(nameof(RightHandSide));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class SimplexTable
    {
        public string TableName { get; set; }
        public ObservableCollection<SimplexRow> Rows { get; set; } = new ObservableCollection<SimplexRow>();
        public List<string> ColumnHeaders { get; set; } = new List<string>();
    }

    public class SimplexRow
    {
        public string BasisVariable { get; set; }
        public double CB { get; set; }
        public ObservableCollection<double> Coefficients { get; set; } = new ObservableCollection<double>();
        public double RightHandSide { get; set; }
        public double? Theta { get; set; }
    }

    public class LinearProgrammingProblem
    {
        public bool IsMaximization { get; set; } = true;
        public ObservableCollection<Coefficient> ObjectiveCoefficients { get; set; } = new ObservableCollection<Coefficient>();
        public ObservableCollection<Constraint> Constraints { get; set; } = new ObservableCollection<Constraint>();
    }

    public class SolutionStep
    {
        public string Description { get; set; }
        public double[,] Table { get; set; }
        public int[] Basis { get; set; }
        public double[] CB { get; set; }
        public int PivotRow { get; set; } = -1;
        public int PivotColumn { get; set; } = -1;
    }
}