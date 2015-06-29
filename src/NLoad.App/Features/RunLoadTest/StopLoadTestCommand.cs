using System;
using System.ComponentModel;
using System.Windows.Input;

namespace NLoad.App.Features.RunLoadTest
{
    public class StopLoadTestCommand : ICommand
    {
        private readonly LoadTestViewModel _loadTestViewModel;
        private readonly BackgroundWorker _worker;

        public StopLoadTestCommand(LoadTestViewModel loadTestViewModel, BackgroundWorker worker)
        {
            _loadTestViewModel = loadTestViewModel;
            _worker = worker;
        }

        public bool CanExecute(object parameter)
        {
            return true;//_worker != null && _worker.IsBusy;
        }

        public void Execute(object parameter)
        {
            _loadTestViewModel.LoadTest.Cancel();
            //if (_worker != null && _worker.IsBusy)
            //{
            //    _worker.CancelAsync();
            //}
        }

        public event EventHandler CanExecuteChanged;
    }
}