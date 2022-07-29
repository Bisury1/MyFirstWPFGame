using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml.Serialization;
using MyGame.Annotations;

namespace MyGame
{
    internal class ApplicationViewModel : INotifyPropertyChanged
    {
        private int _score;
        private List<HighScoreForSerialize> _highScore = new List<HighScoreForSerialize>();
        private ObservableCollection<ObservableCollection<Cell>> _gameField;
        private Game _game;
        private string _userName;
        private bool _gameIsOver;
        private bool _gameIsRunning;
        private ICommand _moveCommand;
        private ICommand _startCommand;
        private ICommand _showRecord;
        private string _textForTextBox;
        private readonly string _path = ConfigurationSettings.AppSettings["RecordFile"];
        public int CurrentScore
        {
            get => _score;
            set
            {
                _score = value;
                if (_userName != null)
                {
                    if (_score > ReturnCurrentUser().HighScore)
                    {
                        HighScore = _score;
                    }
                }

                OnPropertyChanged();
            }
        }

        private HighScoreForSerialize ReturnCurrentUser()
        {

            var item = _highScore.Find(x => _userName == x.userName);
            if (item == null)
            {
                item = new HighScoreForSerialize(_userName);
                _highScore.Add(item);
            }
            return item;
        }

        public int HighScore
        {
            get
            {
                if (_userName != null)
                {
                    var user = ReturnCurrentUser();
                    user = ReturnCurrentUser();
                    return user.HighScore;
                }

                return 0;
            }
            set
            {
                if (_userName != null)
                {
                    var user = ReturnCurrentUser();
                    user.HighScore = value;
                }

                OnPropertyChanged();
            }
        }

        public ObservableCollection<ObservableCollection<Cell>> Field
        {
            get => _gameField;
            set
            {
                _gameField = value;
                OnPropertyChanged();
            }
        }

        public bool GameOver
        {
            get => _gameIsOver;
            set
            {
                _gameIsOver = value;
                OnPropertyChanged();
            }
        }

        public string UserName
        {
            get => _userName;
            set
            {
                _userName = value;
                OnPropertyChanged();
            }
        }
        public bool GameRunning
        {
            get => _gameIsRunning;
            set
            {
                _gameIsRunning = value;
                OnPropertyChanged();
            }
        }

        public void EndGame()
        {
            TextForTextBox = "";
            GameRunning = false;
            GameOver = true;
            if (ReturnCurrentUser().HighScore < CurrentScore)
            {
                var user = ReturnCurrentUser();
                user.HighScore = CurrentScore;
            }
            var bs = new XmlSerializer(typeof(List<HighScoreForSerialize>));
            using (Stream sw = new FileStream(_path, FileMode.Create, FileAccess.Write))
            {
                bs.Serialize(sw, _highScore);
            }
        }

        private void NewGame()
        {
            if (GameRunning)
                _game.Stop();
            var bs = new XmlSerializer(typeof(List<HighScoreForSerialize>));
            try
            {
                using (Stream sw = new FileStream(_path, FileMode.Open, FileAccess.Read))
                {
                    if (sw.Length > 0)
                    {
                        _highScore = (List<HighScoreForSerialize>)bs.Deserialize(sw);
                    }
                }
            }
            catch (IOException)
            {
            }
            GameOver = false;
            CurrentScore = 0;
            _game = new Game(this);
            Field = _game.GameField;
        }

        public ApplicationViewModel()
        {
            NewGame();
        }

        public ICommand StartCommand
        {
            get
            {
                return _startCommand ?? (_startCommand = new RelayCommand(parameter =>
                {
                    if (!string.IsNullOrEmpty(_userName))
                    {
                        if (!GameRunning)
                        {
                            if (GameOver)
                            {
                                HighScore = ReturnCurrentUser().HighScore;
                                NewGame();
                            }
                            else
                            {
                                GameRunning = true;
                                HighScore = ReturnCurrentUser().HighScore;
                                _game.Start();
                            }
                        }
                        else
                        {
                            GameRunning = false;
                            _game.Stop();
                        }
                    }
                    else
                    {
                        MessageBox.Show("Сначала введите имя");
                    }
                }));
            }
        }

        public string TextForTextBox
        {
            get => _textForTextBox;
            set
            {
                _textForTextBox = value;
                OnPropertyChanged();
            }
        }
        public ICommand ShowRecord
        {
            get
            {
                return _showRecord ?? (_showRecord = new RelayCommand(parameter =>
                {
                    TextForTextBox = "";
                    var sb = new StringBuilder();
                    foreach (var item in _highScore)
                    {
                        sb.Append($"{item.userName} {item.HighScore}\n");
                    }
                    TextForTextBox = sb.ToString();
                }));
            }
        }

        public ICommand MoveCommand
        {
            get
            {
                return _moveCommand ?? (_moveCommand = new RelayCommand(parameter =>
                {
                    if (GameRunning && Enum.TryParse(parameter.ToString(), out Direction direction))
                    {
                        _game.direction = direction;
                    }
                }));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
