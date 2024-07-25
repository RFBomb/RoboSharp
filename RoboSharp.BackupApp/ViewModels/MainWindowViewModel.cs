using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RoboSharp.Interfaces;
using RoboSharp.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RoboSharp.BackupApp.ViewModels
{
    internal partial class MainWindowViewModel : ObservableObject
    {
        public MainWindowViewModel()
        {
            SingleJobHistory = new JobHistoryViewModel();
            BatchCommandViewModel = new BatchCommandViewModel();
            System.Windows.Input.CommandManager.RequerySuggested += CommandManager_RequerySuggested;
        }

        private void CommandManager_RequerySuggested(object sender, EventArgs e)
        {
            this.BtnAddToRoboQueueCommand.NotifyCanExecuteChanged();
            this.BtnLoadJobCommand.NotifyCanExecuteChanged();
            this.btnLoadRoboQueueCommand.NotifyCanExecuteChanged();
            this.BtnReplaceRoboQueueCommandCommand.NotifyCanExecuteChanged();
            this.BtnSaveJobCommand.NotifyCanExecuteChanged();
            this.BtnStartJobCommand.NotifyCanExecuteChanged();
        }

        public RoboQueueViewModel RoboQueueViewModel { get; } = new RoboQueueViewModel(new RoboQueue("RoboQueue"));
        public CommandGeneratorViewModel CommandGenerator { get; } = new CommandGeneratorViewModel();
        public CommandProgressViewModel SingleJobProgress { get; } = new CommandProgressViewModel();
        public JobHistoryViewModel SingleJobHistory { get; } 
        public BatchCommandViewModel BatchCommandViewModel { get; }

       

        #region < RoboCommand Buttons >

        [RelayCommand(CanExecute = nameof(CanLoadJob))]
        private void BtnLoadJob()
        {
            var FP = new Microsoft.Win32.OpenFileDialog();
            FP.Filter = RoboSharp.JobFile.JOBFILE_DialogFilter;
            FP.Multiselect = false;
            FP.Title = "Select RoboCopy Job File.";
            try
            {
                JobFile JF = null;
                bool? FilePicked = FP.ShowDialog();
                if (FilePicked ?? false)
                    JF = JobFile.ParseJobFile(FP.FileName);
                if (JF != null)
                {
                    var oldCmd = CommandGenerator.GetCommand() as RoboCommand;
                    oldCmd.MergeJobFile(JF);//Perform Merge Test
                    CommandGenerator.LoadCommand(JF);
                }
                else
                {
                    MessageBox.Show("Job File Not Loaded / Not Selected");
                }
            }
            catch
            {

            }
        }
        private bool CanLoadJob() => true;

        [RelayCommand(CanExecute = nameof(CanSaveJob))]
        private async Task BtnSaveJob()
        {
            var FP = new Microsoft.Win32.SaveFileDialog();
            FP.Filter = RoboSharp.JobFile.JOBFILE_DialogFilter;
            FP.Title = "Save RoboCopy Job File.";
            try
            {
                bool? FilePicked = FP.ShowDialog();
                if (FilePicked ?? false)
                {
                    await (CommandGenerator.GetCommand() as RoboCommand).SaveAsJobFile(FP.FileName, true, true);
                }
                else
                {
                    MessageBox.Show("Job File Not Saved");
                }
            }
            catch
            {

            }
        }
        private bool CanSaveJob() => true;

        [RelayCommand(CanExecute = nameof(CanStartJob))]
        private async Task BtnStartJob()
        {
            var copy = CommandGenerator.GetCommand();
            if (copy == null) return;
            SingleJobProgress.BindToCommand(copy);
            var results = await copy.StartAsync();
            SingleJobHistory.AddResults(results);
        }
        private bool CanStartJob() => !SingleJobProgress.IsRunning;

        #endregion

        #region < RoboQueue Buttons >

        [RelayCommand]
        private void BtnAddToRoboQueue()
        {
            IRoboCommand cmd = CommandGenerator.GetCommand();
            if (cmd == null) return;
            RoboQueueViewModel.Command.AddCommand(cmd);
            System.Windows.Input.CommandManager.InvalidateRequerySuggested();
        }
        
        [RelayCommand(CanExecute = nameof(IsRoboQueueItemSelected))]
        private void BtnLoadRoboQueue() => CommandGenerator.LoadCommand(RoboQueueViewModel.SelectedCommand);

        [RelayCommand(CanExecute = nameof(IsRoboQueueItemSelected))]
        private void BtnReplaceRoboQueueCommand()
        {
            IRoboCommand cmd = CommandGenerator.GetCommand();
            if (cmd == null) return;
            int i = RoboQueueViewModel.Command.IndexOf(RoboQueueViewModel.SelectedCommand);
            if (i >= 0)
            {
                RoboQueueViewModel.Command.ReplaceCommand(cmd, i);
            }
            else
                RoboQueueViewModel.Command.AddCommand(cmd);
        }
        private bool IsRoboQueueItemSelected() => RoboQueueViewModel.SelectedCommand != null;

        #endregion

    }
}
