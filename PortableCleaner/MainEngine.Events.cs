using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PortableCleaner
{
    public partial class MainEngine
    {
        void InitEvents()
        {
            window.Closing += Window_Closing;

            window.btn_JogUP.PreviewMouseLeftButtonDown += Btn_JogUP_MouseLeftButtonDown;
            window.btn_JogDown.PreviewMouseLeftButtonDown += Btn_JogDown_MouseLeftButtonDown;
            window.btn_JogLeft.PreviewMouseLeftButtonDown += Btn_JogLeft_MouseLeftButtonDown;
            window.btn_JogRight.PreviewMouseLeftButtonDown += Btn_JogRight_MouseLeftButtonDown;

            window.btn_JogUP.PreviewMouseLeftButtonUp += Btn_JogUP_MouseLeftButtonUp;
            window.btn_JogDown.PreviewMouseLeftButtonUp += Btn_JogDown_MouseLeftButtonUp;
            window.btn_JogLeft.PreviewMouseLeftButtonUp += Btn_JogLeft_MouseLeftButtonUp;
            window.btn_JogRight.PreviewMouseLeftButtonUp += Btn_JogRight_MouseLeftButtonUp;

            window.btn_SetOrigin.PreviewMouseLeftButtonDown += Btn_SetOrigin_PreviewMouseLeftButtonDown;
            window.btn_SetOrigin.PreviewMouseLeftButtonUp += Btn_SetOrigin_PreviewMouseLeftButtonUp;

            window.btn_XHome.PreviewMouseLeftButtonDown += Btn_XHome_PreviewMouseLeftButtonDown;
            window.btn_XHome.PreviewMouseLeftButtonUp += Btn_XHome_PreviewMouseLeftButtonUp;

            window.btn_ZHome.PreviewMouseLeftButtonDown += Btn_ZHome_PreviewMouseLeftButtonDown;
            window.btn_ZHome.PreviewMouseLeftButtonUp += Btn_ZHome_PreviewMouseLeftButtonUp;

            window.btn_MoveX.PreviewMouseLeftButtonDown += Btn_MoveX_PreviewMouseLeftButtonDown;
            window.btn_MoveX.PreviewMouseLeftButtonUp += Btn_MoveX_PreviewMouseLeftButtonUp;
            window.btn_MoveZ.PreviewMouseLeftButtonDown += Btn_MoveZ_PreviewMouseLeftButtonDown;
            window.btn_MoveZ.PreviewMouseLeftButtonUp += Btn_MoveZ_PreviewMouseLeftButtonUp;

            window.btn_SetJogXSpeed.PreviewMouseLeftButtonDown += Btn_SetJogXSpeed_PreviewMouseLeftButtonDown;
            window.btn_SetJogZSpeed.PreviewMouseLeftButtonDown += Btn_SetJogZSpeed_PreviewMouseLeftButtonDown;

            window.btn_SetMoveXSpeed.PreviewMouseLeftButtonDown += Btn_SetMoveXSpeed_PreviewMouseLeftButtonDown;
            window.btn_SetMoveZSpeed.PreviewMouseLeftButtonDown += Btn_SetMoveZSpeed_PreviewMouseLeftButtonDown;

            window.Btn_NozelForword.PreviewMouseLeftButtonDown += Btn_NozelForword_PreviewMouseLeftButtonDown;
            window.Btn_NozelForword.PreviewMouseLeftButtonUp += Btn_NozelForword_PreviewMouseLeftButtonUp;
            window.Btn_NozelBackword.PreviewMouseLeftButtonDown += Btn_NozelBackword_PreviewMouseLeftButtonDown;
            window.Btn_NozelBackword.PreviewMouseLeftButtonUp += Btn_NozelBackword_PreviewMouseLeftButtonUp;

            window.Btn_NozleCleaningStart.PreviewMouseLeftButtonDown += Btn_NozleCleaningStart_PreviewMouseLeftButtonDown;
            window.Btn_NozleCleaningStart.PreviewMouseLeftButtonUp += Btn_NozleCleaningStart_PreviewMouseLeftButtonUp;

            window.btn_AutoModeXHome.PreviewMouseLeftButtonDown += Btn_XHome_PreviewMouseLeftButtonDown;
            window.btn_AutoModeXHome.PreviewMouseLeftButtonUp += Btn_XHome_PreviewMouseLeftButtonUp;
            window.btn_AutoModeJogLeft.PreviewMouseLeftButtonUp += Btn_JogLeft_MouseLeftButtonUp;
            window.btn_AutoModeJogLeft.PreviewMouseLeftButtonDown += Btn_JogLeft_MouseLeftButtonDown;
            window.btn_AutoModeJogRight.PreviewMouseLeftButtonUp += Btn_JogRight_MouseLeftButtonUp;
            window.btn_AutoModeJogRight.PreviewMouseLeftButtonDown += Btn_JogRight_MouseLeftButtonDown;

            window.btn_AutoModeMoveX.PreviewMouseLeftButtonDown += Btn_MoveX_PreviewMouseLeftButtonDown;
            window.btn_AutoModeMoveX.PreviewMouseLeftButtonUp += Btn_MoveX_PreviewMouseLeftButtonUp;
            window.btn_AutoModeSetJogXSpeed.PreviewMouseLeftButtonDown += Btn_SetJogXSpeed_PreviewMouseLeftButtonDown;
            window.btn_AutoModeSetMoveXSpeed.PreviewMouseLeftButtonDown += Btn_SetMoveXSpeed_PreviewMouseLeftButtonDown;

            window.btn_AutoModeZHome.PreviewMouseLeftButtonDown += Btn_ZHome_PreviewMouseLeftButtonDown;
            window.btn_AutoModeZHome.PreviewMouseLeftButtonUp += Btn_ZHome_PreviewMouseLeftButtonUp;
            window.btn_AutoModeJogUP.PreviewMouseLeftButtonDown += Btn_JogUP_MouseLeftButtonDown;
            window.btn_AutoModeJogUP.PreviewMouseLeftButtonUp += Btn_JogUP_MouseLeftButtonUp;
            window.btn_AutoModeJogDown.PreviewMouseLeftButtonDown += Btn_JogDown_MouseLeftButtonDown;
            window.btn_AutoModeJogDown.PreviewMouseLeftButtonUp += Btn_JogDown_MouseLeftButtonUp;

            window.btn_ArmReset.PreviewMouseLeftButtonDown += Btn_ArmReset_PreviewMouseLeftButtonDown;
            window.btn_ArmReset.PreviewMouseLeftButtonUp += Btn_ArmReset_PreviewMouseLeftButtonUp; ;

            window.btn_Emg.PreviewMouseLeftButtonDown += Btn_Emg_PreviewMouseLeftButtonDown;
        }

        private bool isEmergency = false;

        private void Btn_Emg_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (isEmergency)
            {
                EmergencyOff();

                isEmergency = false;

                btn_Emg_Text = "비상정지 ON";
            }
            else
            {
                EmergencyOn();

                isEmergency = true;

                btn_Emg_Text = "비상정지 OFF";
            }
        }

        private void Btn_ArmReset_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            AlramResetOff();
        }

        private void Btn_ArmReset_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            AlramResetOn();
        }

        private void Btn_NozleCleaningStart_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            NozleStartCleaningOff();
        }

        private void Btn_NozleCleaningStart_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            NozleStartCleaningOn();
        }

        private void Btn_NozelBackword_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            NozleBackwordOff();
        }

        private void Btn_NozelBackword_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            NozleBackwordOn();
        }

        private void Btn_NozelForword_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            NozleForwordOff();
        }

        private void Btn_NozelForword_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            NozleForwordOn();
        }



        private void Btn_SetMoveZSpeed_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SetMoveZSpeed(zMoveSpeed);
        }

        private void Btn_SetMoveXSpeed_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SetMoveXSpeed(xMoveSpeed);
        }

        private void Btn_SetJogZSpeed_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SetJogZSpeed(zJogSpeed);
        }
         
        private void Btn_SetJogXSpeed_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SetJogXSpeed(xJogSpeed);
        }

        private void Btn_MoveZ_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            EndMove();
        }

        private void Btn_MoveZ_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            int xValue = (int)(Convert.ToDouble(currentMotorX) * 100);
            int zValue = (int)(zDestination * 100);

            Move(xValue, zValue);
        }

        private void Btn_MoveX_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            EndMove();
        }

        private void Btn_MoveX_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            int zValue = (int)(Convert.ToDouble(currentMotorZ) * 100);
            int xValue = (int)(xDestination * 100);


            Move(xValue, zValue);
        }

        private void Btn_ZHome_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            EndMove();
        }

        private void Btn_ZHome_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            int xValue = (int)(Convert.ToDouble(currentMotorX) * 100);

            Move(xValue, 0);
          
        }

        private void Btn_XHome_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            EndMove();

        }

        private void Btn_XHome_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            int zValue = (int)(Convert.ToDouble(currentMotorZ) * 100);
            Move(0, zValue);
        }

        private void Btn_SetOrigin_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ReleaseOrigin();
           
        }

        private void Btn_SetOrigin_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SetOrigin();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            isClosing = true;
        }

        

        private void Btn_JogRight_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SetJogRight();
        }

        private void Btn_JogLeft_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SetJogLeft();
        }

        private void Btn_JogDown_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SetJogDown();
        }

        private void Btn_JogUP_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SetJogUp();
        }

        private void Btn_JogRight_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ReleaseJogRight();
        }

        private void Btn_JogLeft_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ReleaseJogLeft();
        }

        private void Btn_JogDown_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ReleaseJogDown();
        }

        private void Btn_JogUP_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ReleaseJogUp();
        }
    }
}
