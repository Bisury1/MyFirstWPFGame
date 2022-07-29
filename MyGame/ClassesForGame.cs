using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace MyGame
{
    #region RelayCommand
    internal class RelayCommand: ICommand
    {
        private Action<object> execute;
        private Func<object, bool> canExecute;

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return this.canExecute == null || this.canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            this.execute(parameter);
        }
    }
    #endregion

    #region EnumeratorForGame

    internal enum Direction
    {
        Right,
        Up,
        Left,
        Down
    }

    internal enum TypeOfCell
    {
        Empty,
        Food,
        Snake
    }

    internal struct Coord
    {
        public int X { get; set; }
        public int Y { get; set; }

        public Coord(int x, int y)
        {
            X = x;
            Y = y;
        }
    }


    #endregion
    #region ClassesForMyGame
    [Serializable]
    public class HighScoreForSerialize
    {
        public int HighScore { get; set; }
        public string userName { get; set; }

        public HighScoreForSerialize(string user)
        {
            userName = user;
        }

        public HighScoreForSerialize()
        {
        }
    }
    internal class Cell: INotifyPropertyChanged
    {
        private TypeOfCell _type;

        public TypeOfCell typeOfCell
        {
            get => _type;
            set
            {
                _type = value;
                OnPropertyChanged();
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    internal class Snake
    {
        private ObservableCollection<ObservableCollection<Cell>> _gameField;
        private Queue<Coord> _snakeBody = new Queue<Coord>();
        private Game _game;
        private Coord _snakeHead;
        private int _length;
        public bool IsDied { get; set; }

        public Snake()
        {
        }
        public Snake(ObservableCollection<ObservableCollection<Cell>> gameField, Game game)
        {
            _game = game;
            _gameField = gameField; 
            SnakeHead = new Coord(gameField[0].Count / 2, gameField.Count / 2);
            _length = 0;
        }

        private int ModField(int number, int count)
        {
            if (number < 0)
            {
                return number + count;
            }

            if (number >= count)
            {
                return number % count;
            }

            return number;
        }
        public void Move(Direction direction)
        {
            Coord nextCoord = new Coord();
            switch (direction)
            {
                case Direction.Right:
                    nextCoord = new Coord(ModField((_snakeHead.X + 1),_gameField.Count), ModField((_snakeHead.Y),_gameField[0].Count));
                    break;
                case Direction.Up:
                    nextCoord = new Coord(ModField((_snakeHead.X), _gameField.Count), ModField((_snakeHead.Y - 1), _gameField[0].Count));
                    break;
                case Direction.Left:
                    nextCoord = new Coord(ModField((_snakeHead.X - 1), _gameField.Count), ModField((_snakeHead.Y), _gameField[0].Count));
                    break;
                case Direction.Down:
                    nextCoord = new Coord(ModField((_snakeHead.X), _gameField.Count), ModField((_snakeHead.Y + 1), _gameField[0].Count));
                    break;
            }
            if (!CheckMove(nextCoord))
                return;
            _snakeBody.Enqueue(_snakeHead);
            SnakeHead = nextCoord;
            if (_length < _snakeBody.Count)
            {
                var delCell = _snakeBody.Dequeue();
                _gameField[delCell.X][delCell.Y].typeOfCell = TypeOfCell.Empty;
            }
        }

        public bool CheckMove(Coord coord)
        {
            if (_snakeBody.Contains(coord))
            {
                IsDied = true;
                return false;
            }
            if(_gameField[coord.X][coord.Y].typeOfCell == TypeOfCell.Food)
            {
                _gameField[coord.X][coord.Y].typeOfCell = TypeOfCell.Empty;
                _game.AddFood();
                _length++;
            }

            return true;
        }

        public Coord SnakeHead
        {
            get => _snakeHead;
            set
            {
                _snakeHead = value;
                _gameField[value.X][value.Y].typeOfCell = TypeOfCell.Snake;
            }
        }
    }

    internal class Game
    {
        private ApplicationViewModel _viewModel;
        private Snake _snake;
        private Direction _direction;
        private CancellationTokenSource _cts = null;
        private short _timer = 200;
        private bool _delayAfterChangeDirection;
        Random rnd = new Random();
        public ObservableCollection<ObservableCollection<Cell>> GameField { get; set; }

        public Game(ApplicationViewModel viewModel)
        {
            _viewModel = viewModel;
            GameField = new ObservableCollection<ObservableCollection<Cell>>();
            for (int i = 0; i < 29; i++)
            {
                GameField.Add(new ObservableCollection<Cell>());
                for (int j = 0; j < 27; j++)
                {
                    GameField[i].Add(new Cell());
                }
            }

            GameField[15][12].typeOfCell = TypeOfCell.Food;
            _snake = new Snake(GameField, this);
        }

        public void Stop()
        {
            _cts?.Cancel();
        }

        public void Start()
        {
            if (_cts == null)
                Run();
        }

        private async void Run()
        {
            using (_cts = new CancellationTokenSource())
            {
                try
                {
                    while (true)
                    {
                        if (_snake.IsDied)
                        {
                            _viewModel.EndGame();
                            break;
                        }
                        Update();
                        await Task.Delay(_timer, _cts.Token);
                        if (_delayAfterChangeDirection)
                        {
                            _delayAfterChangeDirection = false;
                            await Task.Delay(_timer / 3, _cts.Token);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }

            _cts = null;

        }

        public void Update()
        {
            _snake.Move(direction);
        }

        public void AddFood()
        {
            while (true)
            {
                var coord = new Coord(rnd.Next(0, GameField.Count), rnd.Next(GameField[0].Count));
                if (GameField[coord.X][coord.Y].typeOfCell != TypeOfCell.Empty) continue;
                GameField[coord.X][coord.Y].typeOfCell = TypeOfCell.Food;
                _viewModel.CurrentScore += 20;
                break;
            }
        }

        public Direction direction
        {
            get => _direction;
            set
            {
                if ((int) value % 2 != (int) _direction % 2 && value != _direction)
                {
                    _direction = value;
                    _delayAfterChangeDirection = true;
                    Update();
                }
            }
        }
    }


    #endregion
}
