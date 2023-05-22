using PortableCleaner.Struct;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace PortableCleaner
{
    public class InspectionInfo : ViewModel
    {
        public double scanXDistance;
        public double scanZDistance;

        public short[] data;
        public long width;
        public long length;
        public double xResolution;
        public double yResolution;
        public double zResolution;

        private ObservableCollection<InspectionManager.StructBlob> findCircle = new ObservableCollection<InspectionManager.StructBlob>();
        public ObservableCollection<InspectionManager.StructBlob> FindCircle
        {
            get { return findCircle; }
            set { findCircle = value; NotifyPropertyChanged("FindCircle"); }
        }

        private string bundleName;
        public string BundleName { get { return bundleName; } set { bundleName = value; NotifyPropertyChanged("BundleName"); } }

        private int bundleLength;
        public int BundleLength { get { return bundleLength; } set { bundleLength = value; NotifyPropertyChanged("BundleLength"); } }

        private ObservableCollection<StructHole> holes = new ObservableCollection<StructHole>();
        public ObservableCollection<StructHole> Holes { get { return holes; } set { holes = value; NotifyPropertyChanged("Holes"); } }

        public BitmapSource HoleSettingOriginalImage { get; set; }

        private BitmapSource holeSettingImage;
        public BitmapSource HoleSettingImage { get { return holeSettingImage; } set { holeSettingImage = value; NotifyPropertyChanged("HoleSettingImage"); } }

        private BitmapSource cleaningImage;
        public BitmapSource CleaningImage { get { return cleaningImage; } set { cleaningImage = value; NotifyPropertyChanged("CleaningImage"); } }

        private string cleaningPerStr;
        public string CleaningPerStr { get { return cleaningPerStr; } set { cleaningPerStr = value; NotifyPropertyChanged("CleaningPerStr"); } }

        private int cleaningCount;
        public int CleaningCount { get { return cleaningCount; } set { cleaningCount = value; 
                NotifyPropertyChanged("CleaningCount");
                NotifyPropertyChanged("NoCleaningCount");
                NotifyPropertyChanged("CleaningHoleCount");
                NotifyPropertyChanged("CleaningOkHoleCount");
                NotifyPropertyChanged("CleaningNGHoleCount");

            } }

        private int cleaningMaxCount;
        public int CleaningMaxCount { get { return cleaningMaxCount; } set { cleaningMaxCount = value; NotifyPropertyChanged("CleaningMaxCount"); } }
         
        public int CleaningHoleCount
        {
            get { return holes.Count; }
        }

        public int CleaningOkHoleCount {
            get 
            {
                int count = 0;
                for (int i = 0; i < Holes.Count; i++)
                {
                    StructHole hole = Holes[i];
                    if (hole.IsOK == true)
                    {
                        count++;
                    }
                }

                return count;
            }
             
        }

        public int CleaningNGHoleCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < Holes.Count; i++)
                {
                    StructHole hole = Holes[i];
                    if (hole.IsOK == false)
                    {
                        count++;
                    }
                }

                return count;
            }

        }

        public int NoCleaningCount 
        { 
            get 
            {
                int count = 0;
                for (int i = 0; i < Holes.Count; i++)
                {
                    StructHole hole = Holes[i];
                    if (hole.IsOK == false)
                    {
                        count++;
                    }
                }

                return count;
            } 
        }

        private bool isUseNozle = true;
        public bool IsUseNozle { get { return isUseNozle; } set { isUseNozle = value; NotifyPropertyChanged("IsUseNozle"); } }

        private bool isFinishScanning = false;
        public bool IsFinishScanning { get { return isFinishScanning; } set { isFinishScanning = value; NotifyPropertyChanged("IsFinishScanning"); } }


        private bool isFinishScanningIntensityImage = false;
        public bool IsFinishScanningIntensityImage { get { return isFinishScanningIntensityImage; } set { isFinishScanningIntensityImage = value; NotifyPropertyChanged("IsFinishScanningIntensityImage"); } }


        public BitmapSource appImage;

        public double appImageWidth { get; set; }
        public double appImageHeight { get; set; }

        private DateTime cleaningStartDateTime;
        public DateTime CleaningStartDateTime { get { return cleaningStartDateTime; } set { cleaningStartDateTime = value; NotifyPropertyChanged("CleaningStartDateTime"); NotifyPropertyChanged("CleaingTime"); NotifyPropertyChanged("CleaingTimeStr"); NotifyPropertyChanged("CleaningStartDateTimeStr"); } }

        public string CleaningStartDateTimeStr { get { return cleaningStartDateTime.ToString("yyyy-MM-dd HH:mm:ss"); } }

        private DateTime cleaningEndDateTime;
        public DateTime CleaningEndDateTime { get { return cleaningEndDateTime; } set { cleaningEndDateTime = value; NotifyPropertyChanged("CleaningEndDateTime"); NotifyPropertyChanged("CleaningTime"); NotifyPropertyChanged("CleaningTimeStr"); NotifyPropertyChanged("CleaningEndDateTimeStr"); } }

        public string CleaningEndDateTimeStr { get { return cleaningEndDateTime.ToString("yyyy-MM-dd HH:mm:ss"); } }

        private TimeSpan CleaningTime
        {
            get { return (CleaningEndDateTime - CleaningStartDateTime); }
        }

        public string CleaningTimeStr { get { return CleaningTime.ToString("h'h 'm'm 's's'"); } }

        private int currentCleaingHoleIndex;
        public int CurrentCleaingHoleIndex { get{ return currentCleaingHoleIndex; } set { currentCleaingHoleIndex = value; NotifyPropertyChanged("CurrentCleaingHoleIndex"); } }
    }
}
