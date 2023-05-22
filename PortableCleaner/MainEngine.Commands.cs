using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PortableCleaner
{
    public partial class MainEngine
    {
        private ICommand setOriginLeftTopCommand;
        public ICommand SetOriginLeftTopCommand
        {
            get { return (this.setOriginLeftTopCommand) ?? (this.setOriginLeftTopCommand = new DelegateCommand(SetOriginLeftTop)); }
        }

        private ICommand setOriginRightTopCommand;
        public ICommand SetOriginRightTopCommand
        {
            get { return (this.setOriginRightTopCommand) ?? (this.setOriginRightTopCommand = new DelegateCommand(SetOriginRightTop)); }
        }


        private ICommand setOriginLeftBottomCommand;
        public ICommand SetOriginLeftBottomCommand
        {
            get { return (this.setOriginLeftBottomCommand) ?? (this.setOriginLeftBottomCommand = new DelegateCommand(SetOriginLeftBottom)); }
        }


        private ICommand setOriginRightBottomCommand;
        public ICommand SetOriginRightBottomCommand
        {
            get { return (this.setOriginRightBottomCommand) ?? (this.setOriginRightBottomCommand = new DelegateCommand(SetOriginRightBottom)); }
        }

        private ICommand startScanCommand;
        public ICommand StartScanCommand
        {
            get { return (this.startScanCommand) ?? (this.startScanCommand = new DelegateCommand(StartScan)); }
        }

        private ICommand moveSemiAutoModeCommand;
        public ICommand MoveSemiAutoModeCommand
        {
            get { return (this.moveSemiAutoModeCommand) ?? (this.moveSemiAutoModeCommand = new RelayCommand<object>(MoveSemiAutoMode)); }
        }


        private ICommand confirmHoleSettingCommand;
        public ICommand ConfirmHoleSettingCommand
        {
            get { return (this.confirmHoleSettingCommand) ?? (this.confirmHoleSettingCommand = new DelegateCommand(ConfirmHoleSetting)); }
        }

        private ICommand manualLazerOnCommand;
        public ICommand ManualLazerOnCommand
        {
            get { return (this.manualLazerOnCommand) ?? (this.manualLazerOnCommand = new DelegateCommand(ManualLazerOn)); }
        }
         
        private ICommand nozleOnCommand;
        public ICommand NozleOnCommand
        {
            get { return (this.nozleOnCommand) ?? (this.nozleOnCommand = new DelegateCommand(NozleOn)); }
        }

        private ICommand nozleOffCommand;
        public ICommand NozleOffCommand
        {
            get { return (this.nozleOffCommand) ?? (this.nozleOffCommand = new DelegateCommand(NozleOff)); }
        }

        private ICommand setNozleForwordTimeCommand;
        public ICommand SetNozleForwordTimeCommand
        {
            get { return (this.setNozleForwordTimeCommand) ?? (this.setNozleForwordTimeCommand = new DelegateCommand(SetNozleForwordTime)); }
        }


        private ICommand setNozleBackwordTimeCommand;
        public ICommand SetNozleBackwordTimeCommand
        {
            get { return (this.setNozleBackwordTimeCommand) ?? (this.setNozleBackwordTimeCommand = new DelegateCommand(SetNozleBackwordTime)); }
        }


        private ICommand moveInterLockOnCommand;
        public ICommand MoveInterLockOnCommand
        {
            get { return (this.moveInterLockOnCommand) ?? (this.moveInterLockOnCommand = new DelegateCommand(MoveInterLockOn)); }
        }

        private ICommand moveInterLockOffCommand;
        public ICommand MoveInterLockOffCommand
        {
            get { return (this.moveInterLockOffCommand) ?? (this.moveInterLockOffCommand = new DelegateCommand(MoveInterLockOff)); }
        }

        private ICommand autoModeRemoveHoleCommand;
        public ICommand AutoModeRemoveHoleCommand
        {
            get { return (this.autoModeRemoveHoleCommand) ?? (this.autoModeRemoveHoleCommand = new DelegateCommand(AutoModeRemoveHole)); }
        }

        private ICommand stopCleaningCommand;
        public ICommand StopCleaningCommand
        {
            get { return (this.stopCleaningCommand) ?? (this.stopCleaningCommand = new DelegateCommand(StopCleaning)); }
        }

        private ICommand skipOneHoleCommand;
        public ICommand SkipOneHoleCommand
        {
            get { return (this.skipOneHoleCommand) ?? (this.skipOneHoleCommand = new DelegateCommand(SkipOneHole)); }
        }

        private ICommand pumpOnCommand;
        public ICommand PumpOnCommand
        {
            get { return (this.pumpOnCommand) ?? (this.pumpOnCommand = new DelegateCommand(PumpOn)); }
        }

        private ICommand pumpOffCommand;
        public ICommand PumpOffCommand
        {
            get { return (this.pumpOffCommand) ?? (this.pumpOffCommand = new DelegateCommand(PumpOff)); }
        }

    }
}
