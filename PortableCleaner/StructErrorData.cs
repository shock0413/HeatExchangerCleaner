using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PortableCleaner
{
    public class StructErrorData : INotifyPropertyChanged
    {
        public enum ErrorPriority { Low, Middle, High }

        public int ID { get { return id; } set { id = value; NotifyPropertyChanged("ID"); } }
        private int id = 0;

        public string Command { get { return command; } set { command = value; NotifyPropertyChanged("Command"); } }
        private string command = "";

        public string DateTime { get { return dateTime; } set { dateTime = value; NotifyPropertyChanged("DateTime"); } }
        private string dateTime = "";

        public string Title { get { return title; } set { title = value; NotifyPropertyChanged("Title"); } }
        private string title = "";

        public string Content { get { return content; } set { content = value; NotifyPropertyChanged("Content"); } }
        private string content = "";

        public string ActionContent { get { return actionContent; } set { actionContent = value; NotifyPropertyChanged("ActionContent"); } }
        private string actionContent = "";

        public bool IsCleared { get { return isCleared; } set { isCleared = value; NotifyPropertyChanged("IsCleared"); } }
        private bool isCleared = false;

        public string ClearDateTime { get { return clearDateTime; } set { clearDateTime = value; NotifyPropertyChanged("ClearDateTime"); } }
        private string clearDateTime = "";

        private string priority = "";
        public string Priority { get { return priority; } set { priority = value; } }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
