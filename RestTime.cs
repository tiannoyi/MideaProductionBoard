using System.ComponentModel;

namespace MideaProductionBoard
{
    public class RestTime : INotifyPropertyChanged
    {
        private int _startHour;
        private int _startMinute;
        private int _endHour;
        private int _endMinute;

        public int StartHour
        {
            get => _startHour;
            set
            {
                if (_startHour != value)
                {
                    _startHour = value;
                    OnPropertyChanged(nameof(StartHour));
                }
            }
        }

        public int StartMinute
        {
            get => _startMinute;
            set
            {
                if (_startMinute != value)
                {
                    _startMinute = value;
                    OnPropertyChanged(nameof(StartMinute));
                }
            }
        }

        public int EndHour
        {
            get => _endHour;
            set
            {
                if (_endHour != value)
                {
                    _endHour = value;
                    OnPropertyChanged(nameof(EndHour));
                }
            }
        }

        public int EndMinute
        {
            get => _endMinute;
            set
            {
                if (_endMinute != value)
                {
                    _endMinute = value;
                    OnPropertyChanged(nameof(EndMinute));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public RestTime Clone()
        {
            return new RestTime
            {
                StartHour = this.StartHour,
                StartMinute = this.StartMinute,
                EndHour = this.EndHour,
                EndMinute = this.EndMinute
            };
        }
    }
}