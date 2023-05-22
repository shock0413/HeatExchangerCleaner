using PortableCleaner.Struct;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PortableCleaner
{
    public partial class MainEngine : ViewModel
    {
        public double OffsetX { get { return iniConfig.GetDouble("Info", "OffsetX", 0); } set { iniConfig.WriteValue("Info", "OffsetX", value); NotifyPropertyChanged("OffsetX"); }  }
        public double OffsetY { get { return iniConfig.GetDouble("Info", "OffsetY", 0); } set { iniConfig.WriteValue("Info", "OffsetY", value); NotifyPropertyChanged("OffsetY"); } }

        private string currentMotorX;
        public string CurrentMotorX { get { return currentMotorX; } set { currentMotorX = value; NotifyPropertyChanged("CurrentMotorX"); } }

        private string currentMotorZ;
        public string CurrentMotorZ { get { return currentMotorZ; } set { currentMotorZ = value; NotifyPropertyChanged("CurrentMotorZ"); } }

        private string state;
        public string State { get { return state; } set { state = value; NotifyPropertyChanged("State"); } }

        private double xDestination;
        public double XDestination { get { return xDestination; } set { xDestination = value; NotifyPropertyChanged("XDestination"); } }

        private double zDestination;
        public double ZDestination { get { return zDestination; } set { zDestination = value; NotifyPropertyChanged("ZDestination"); } }

        private double originLeftTopX;
        public double OriginLeftTopX { get { return originLeftTopX; } set { originLeftTopX = value; NotifyPropertyChanged("OriginLeftTopX"); } }

        private double originLeftBottomX;
        public double OriginLeftBottomX { get { return originLeftBottomX; } set { originLeftBottomX = value; NotifyPropertyChanged("OriginLeftBottomX"); } }

        private double originRightTopX;
        public double OriginRightTopX { get { return originRightTopX; } set { originRightTopX = value; NotifyPropertyChanged("OriginRightTopX"); } }

        private double originRightBottomX;
        public double OriginRightBottomX { get { return originRightBottomX; } set { originRightBottomX = value; NotifyPropertyChanged("OriginRightBottomX"); } }

        private double originLeftTopZ;
        public double OriginLeftTopZ { get { return originLeftTopZ; } set { originLeftTopZ = value; NotifyPropertyChanged("OriginLeftTopZ"); } }

        private double originLeftBottomZ;
        public double OriginLeftBottomZ { get { return originLeftBottomZ; } set { originLeftBottomZ = value; NotifyPropertyChanged("OriginLeftBottomZ"); } }

        private double originRightTopZ;
        public double OriginRightTopZ { get { return originRightTopZ; } set { originRightTopZ = value; NotifyPropertyChanged("OriginRightTopZ"); } }

        private double originRightBottomZ;
        public double OriginRightBottomZ { get { return originRightBottomZ; } set { originRightBottomZ = value; NotifyPropertyChanged("OriginRightBottomZ"); } }

        private ushort xJogSpeed;
        public ushort XJogSpeed { get { return xJogSpeed; } set { xJogSpeed = value; NotifyPropertyChanged("XJogSpeed"); } }

        private ushort zJogSpeed;
        public ushort ZJogSpeed { get { return zJogSpeed; } set { zJogSpeed = value; NotifyPropertyChanged("ZJogSpeed"); } }


        private ushort xMoveSpeed;
        public ushort XMoveSpeed { get { return xMoveSpeed; } set { xMoveSpeed = value; NotifyPropertyChanged("XMoveSpeed"); } }

        private ushort zMoveSpeed;
        public ushort ZMoveSpeed { get { return zMoveSpeed; } set { zMoveSpeed = value; NotifyPropertyChanged("ZMoveSpeed"); } }

        private BitmapSource scanImage;
        public BitmapSource ScanImage { get { return scanImage; } set { scanImage = value; NotifyPropertyChanged("ScanImage"); } }

        private InspectionInfo inspectionInfo = new InspectionInfo();
        public InspectionInfo InspectionInfo { get { return inspectionInfo; } set { inspectionInfo = value; NotifyPropertyChanged("InspectionInfo"); } }

        private InspectionManager.StructBlob selectedBlob;
        public InspectionManager.StructBlob SelectedBlob { get { return selectedBlob; } set { selectedBlob = value; NotifyPropertyChanged("SelectedBlob"); } }

        private ushort nozleForwordTime;
        public ushort NozleForwordTime { get { return nozleForwordTime; } set{ nozleForwordTime = value; NotifyPropertyChanged("NozleForwordTime"); } }


        private ushort nozleBackwordTime;
        public ushort NozleBackwordTime { get { return nozleBackwordTime; } set { nozleBackwordTime = value; NotifyPropertyChanged("NozleBackwordTime"); } }

        private string cleaningState = "";
        public string CleaningState { get { return cleaningState; } set { cleaningState = value; NotifyPropertyChanged("CleaningState"); } }

        private string isCleaningFinish = "";
        public string IsCleaningFinish { get { return isCleaningFinish; } set { isCleaningFinish = value; NotifyPropertyChanged("IsCleaningFinish"); } }

        private string nozleErrorState = "";
        public string NozleErrorState { get { return nozleErrorState; } set { nozleErrorState = value; NotifyPropertyChanged("NozleErrorState"); } }

        private string input_HB;
        public string Input_HB { get { return input_HB; } set { input_HB = value; NotifyPropertyChanged("Input_HB"); } }

        private string input_Nozle1Forword;
        public string Input_Nozle1Forword { get { return input_Nozle1Forword; } set { input_Nozle1Forword = value; NotifyPropertyChanged("Input_Nozle1Forword"); } }
        private string input_Nozle2Forword;
        public string Input_Nozle2Forword { get { return input_Nozle2Forword; } set { input_Nozle2Forword = value; NotifyPropertyChanged("Input_Nozle2Forword"); } }
        private string input_Nozle3Forword;
        public string Input_Nozle3Forword { get { return input_Nozle3Forword; } set { input_Nozle3Forword = value; NotifyPropertyChanged("Input_Nozle3Forword"); } }

        private string input_Nozle1Backword;
        public string Input_Nozle1Backword { get { return input_Nozle1Backword; } set { input_Nozle1Backword = value; NotifyPropertyChanged("Input_Nozle1Backword"); } }
        private string input_Nozle2Backword;
        public string Input_Nozle2Backword { get { return input_Nozle2Backword; } set { input_Nozle2Backword = value; NotifyPropertyChanged("Input_Nozle2Backword"); } }
        private string input_Nozle3Backword;
        public string Input_Nozle3Backword { get { return input_Nozle3Backword; } set { input_Nozle3Backword = value; NotifyPropertyChanged("Input_Nozle3Backword"); } }

        private string input_PumpOn;
        public string Input_PumpOn { get { return input_PumpOn; } set { input_PumpOn = value; NotifyPropertyChanged("Input_PumpOn"); } }

        private string input_Emergency;
        public string Input_Emergency { get { return input_Emergency; } set { input_Emergency = value; NotifyPropertyChanged("Input_Emergency"); } }

        private string input_MoveReady;
        public string Input_MoveReady { get { return input_MoveReady; } set { input_MoveReady = value; NotifyPropertyChanged("Input_MoveReady"); } }

        private string input_CleaningFinish;
        public string Input_CleaningFinish { get { return input_CleaningFinish; } set { input_CleaningFinish = value; NotifyPropertyChanged("Input_CleaningFinish"); } }

        private string input_CleaningState;
        public string Input_CleaningState { get { return input_CleaningState; } set { input_CleaningState = value; NotifyPropertyChanged("Input_CleaningState"); } }

        private string input_XPos;
        public string Input_XPos { get { return input_XPos; } set { input_XPos = value; NotifyPropertyChanged("Input_XPos"); } }

        private string input_ZPos;
        public string Input_ZPos { get { return input_ZPos; } set { input_ZPos = value; NotifyPropertyChanged("Input_ZPos"); } }

        private string input_XLimitError;
        public string Input_XLimitError { get { return input_XLimitError; } set { input_XLimitError = value; NotifyPropertyChanged("Input_XLimitError"); } }

        private string input_ZLimitError;
        public string Input_ZLimitError { get { return input_ZLimitError; } set { input_ZLimitError = value; NotifyPropertyChanged("Input_ZLimitError"); } }

        private string input_XServoLoad;
        public string Input_XServoLoad { get { return input_XServoLoad; } set { input_XServoLoad = value; NotifyPropertyChanged("Input_XServoLoad"); } }

        private string input_ZServoLoad;
        public string Input_ZServoLoad { get { return input_ZServoLoad; } set { input_ZServoLoad = value; NotifyPropertyChanged("Input_ZServoLoad"); } }

        private string input_NozleCheckError;
        public string Input_NozleCheckError { get { return input_NozleCheckError; } set { input_NozleCheckError = value; NotifyPropertyChanged("Input_NozleCheckError"); } }


        private string output_HB;
        public string Output_HB { get { return output_HB; } set { output_HB = value; NotifyPropertyChanged("Output_HB"); } }


        private string output_XDesPos;
        public string Output_XDesPos { get { return output_XDesPos; } set { output_XDesPos = value; NotifyPropertyChanged("Output_XDesPos"); } }

        private string output_ZDesPos;
        public string Output_ZDesPos { get { return output_ZDesPos; } set { output_ZDesPos = value; NotifyPropertyChanged("Output_ZDesPos"); } }

        private string output_StartMove;
        public string Output_StartMove { get { return output_StartMove; } set { output_StartMove = value; NotifyPropertyChanged("Output_StartMove"); } }

        private string output_PumpOn;
        public string Output_PumpOn { get { return output_PumpOn; } set { output_PumpOn = value; NotifyPropertyChanged("Output_PumpOn"); } }

        private string output_StartCleaning;
        public string Output_StartCleaning { get { return output_StartCleaning; } set { output_StartCleaning = value; NotifyPropertyChanged("Output_StartCleaning"); } }

        private string output_NozleSelect;
        public string Output_NozleSelect { get { return output_NozleSelect; } set { output_NozleSelect = value; NotifyPropertyChanged("Output_NozleSelect"); } }


        private string output_NozleForwordLimit;
        public string Output_NozleForwordLimit { get { return output_NozleForwordLimit; } set { output_NozleForwordLimit = value; NotifyPropertyChanged("Output_NozleForwordLimit"); } }


        private string output_NozleBackwordLimit;
        public string Output_NozleBackwordLimit { get { return output_NozleBackwordLimit; } set { output_NozleBackwordLimit = value; NotifyPropertyChanged("Output_NozleBackwordLimit"); } }


        private string output_MoveRight;
        public string Output_MoveRight { get { return output_MoveRight; } set { output_MoveRight = value; NotifyPropertyChanged("Output_MoveRight"); } }

        private string output_MoveLeft;
        public string Output_MoveLeft { get { return output_MoveLeft; } set { output_MoveLeft = value; NotifyPropertyChanged("Output_MoveLeft"); } }

        private string output_MoveDown;
        public string Output_MoveDown { get { return output_MoveDown; } set { output_MoveDown = value; NotifyPropertyChanged("Output_MoveDown"); } }

        private string output_MoveUp;
        public string Output_MoveUp { get { return output_MoveUp; } set { output_MoveUp = value; NotifyPropertyChanged("Output_MoveUp"); } }

        private string output_XJogSpeed;
        public string Output_XJogSpeed { get { return output_XJogSpeed; } set { output_XJogSpeed = value; NotifyPropertyChanged("Output_XJogSpeed"); } }

        private string output_ZJogSpeed;
        public string Output_ZJogSpeed { get { return output_ZJogSpeed; } set { output_ZJogSpeed = value; NotifyPropertyChanged("Output_ZJogSpeed"); } }

        private string output_XMoveSpeed;
        public string Output_XMoveSpeed { get { return output_XMoveSpeed; } set { output_XMoveSpeed = value; NotifyPropertyChanged("Output_XMoveSpeed"); } }

        private string output_ZMoveSpeed;
        public string Output_ZMoveSpeed { get { return output_ZMoveSpeed; } set { output_ZMoveSpeed = value; NotifyPropertyChanged("Output_ZMoveSpeed"); } }

        private string output_SetOrigin;
        public string Output_SetOrigin { get { return output_SetOrigin; } set { output_SetOrigin = value; NotifyPropertyChanged("Output_SetOrigin"); } }

        private string output_AlramReset;
        public string Output_AlramReset { get { return output_AlramReset; } set { output_AlramReset = value; NotifyPropertyChanged("Output_AlramReset"); } }

        private string output_NozleForword;
        public string Output_NozleForword { get { return output_NozleForword; } set { output_NozleForword = value; NotifyPropertyChanged("Output_NozleForword"); } }

        private string output_NozleBackword;
        public string Output_NozleBackword { get { return output_NozleBackword; } set { output_NozleBackword = value; NotifyPropertyChanged("Output_NozleBackword"); } }

        private string output_FreeMove;
        public string Output_FreeMove { get { return output_FreeMove; } set { output_FreeMove = value; NotifyPropertyChanged("Output_FreeMove"); } }

        private StructHole selectedAutoModeHole;
        public StructHole SelectedAutoModeHole
        {
            get { return selectedAutoModeHole; }
            set { selectedAutoModeHole = value; NotifyPropertyChanged("SelectedAutoModeHole"); }
        }

        private double xMinLimit = double.MinValue;
        private double xMaxLimit = double.MaxValue;

        private double zMinLimit = double.MinValue;
        private double zMaxLimit = double.MaxValue;

        public double XMinLimit { get { return xMinLimit; } set { xMinLimit = value; NotifyPropertyChanged("XMinLimit"); } }
        public double XMaxLimit { get { return xMaxLimit; } set { xMaxLimit = value; NotifyPropertyChanged("XMaxLimit"); } }
        public double ZMinLimit { get { return zMinLimit; } set { zMinLimit = value; NotifyPropertyChanged("ZMinLimit"); } }
        public double ZMaxLimit { get { return zMaxLimit; } set { zMaxLimit = value; NotifyPropertyChanged("ZMaxLimit"); } }

        private SolidColorBrush nozle1ForwordLEDColor;
        public SolidColorBrush Nozle1ForwordLEDColor { get { return nozle1ForwordLEDColor; } set { nozle1ForwordLEDColor = value; NotifyPropertyChanged("Nozle1ForwordLEDColor"); } }


        private SolidColorBrush nozle2ForwordLEDColor;
        public SolidColorBrush Nozle2ForwordLEDColor { get { return nozle2ForwordLEDColor; } set { nozle2ForwordLEDColor = value; NotifyPropertyChanged("Nozle2ForwordLEDColor"); } }


        private SolidColorBrush nozle3ForwordLEDColor;
        public SolidColorBrush Nozle3ForwordLEDColor { get { return nozle3ForwordLEDColor; } set { nozle3ForwordLEDColor = value; NotifyPropertyChanged("Nozle3ForwordLEDColor"); } }

        private SolidColorBrush nozle1BackwordLEDColor;
        public SolidColorBrush Nozle1BackwordLEDColor { get { return nozle1BackwordLEDColor; } set { nozle1BackwordLEDColor = value; NotifyPropertyChanged("Nozle1BackwordLEDColor"); } }


        private SolidColorBrush nozle2BackwordLEDColor;
        public SolidColorBrush Nozle2BackwordLEDColor { get { return nozle2BackwordLEDColor; } set { nozle2BackwordLEDColor = value; NotifyPropertyChanged("Nozle2BackwordLEDColor"); } }


        private SolidColorBrush nozle3BackwordLEDColor;
        public SolidColorBrush Nozle3BackwordLEDColor { get { return nozle3BackwordLEDColor; } set { nozle3BackwordLEDColor = value; NotifyPropertyChanged("Nozle3BackwordLEDColor"); } }

        private bool isControllSystem;
        public bool IsControllSystem 
        {
            get { return isControllSystem; }
            set { isControllSystem = value; NotifyPropertyChanged("IsControllSystem"); } 
        }

        private bool isPauseCleaning;
        public bool IsPauseCleaning
        {
            get { return isPauseCleaning; }
            set { isPauseCleaning = value; NotifyPropertyChanged("IsPauseCleaning"); }
        }

        private ObservableCollection<StructErrorData> errorList = new ObservableCollection<StructErrorData>();
        public ObservableCollection<StructErrorData> ErrorList { get { return errorList; } set { errorList = value; NotifyPropertyChanged("ErrorList"); } }

        public string btn_Emg_Text { get { return emg_Text; } set { emg_Text = value; NotifyPropertyChanged("btn_Emg_Text"); } }
        private string emg_Text = "비상정지 ON";
    }
}
