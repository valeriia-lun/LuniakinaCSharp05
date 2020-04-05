using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Data;
using Luniakina05.Managers;
using Luniakina05.Models;

namespace Luniakina05.ViewModels
{
    class ProcessViewModel : INotifyPropertyChanged
    {
        #region Fields

        private ConcurrentDictionary<int, ProcessModel> _processesList = new ConcurrentDictionary<int, ProcessModel>();
        private KeyValuePair<int, ProcessModel> _selectedProcess;

        private RelayCommand<object> _openFolderCommand;
        private RelayCommand<object> _stopProcessCommand;

        private CollectionViewSource _viewSource = new CollectionViewSource();

        private Thread _workingThread;
        private Thread _metaDatesThread;

        private CancellationToken _token;
        private CancellationTokenSource _tokenSource;
        #endregion

        #region Constructor
        public ProcessViewModel()
        {
            _tokenSource = new CancellationTokenSource();
            _token = _tokenSource.Token;
            Synchronize();
            StartWorkingThread();
            StationManager.StopThreads += StopWorkingThread;
            ViewSource.Source = _processesList;
        }
        #endregion

        #region Properties
        public CollectionViewSource ViewSource
        {
            get
            {
                KeyValuePair<int, ProcessModel> p = _selectedProcess;
                _viewSource?.View?.Refresh();
                SelectedProcess = p;
                return _viewSource;
            }
        }

        public KeyValuePair<int, ProcessModel> SelectedProcess
        {
            get => _selectedProcess;
            set
            {
                _selectedProcess = value;
                OnPropertyChanged();
                OnPropertyChanged("ProcessModules");
                OnPropertyChanged("ProcessThreads");
            }
        }

        public ProcessModuleCollection ProcessModules => SelectedProcess.Value?.Modules;

        public ProcessThreadCollection ProcessThreads => SelectedProcess.Value?.Threads;
        #endregion

        #region Commands
        public RelayCommand<object> Stop => _stopProcessCommand ?? (_stopProcessCommand = new RelayCommand<object>(
                                                o => StopProcessImplementation(), o => CanExecute()));

        public RelayCommand<object> OpenFolder => _openFolderCommand ?? (_openFolderCommand = new RelayCommand<object>(
                                                      o => OpenImplementation(), o => CanExecuteFolder()));

        private void OpenImplementation()
        {
            Process.Start("explorer.exe", "/select, \"" + SelectedProcess.Value.FilePath + "\"");
        }

        private void StopProcessImplementation()
        {
            SelectedProcess.Value.StopProcess();
            OnPropertyChanged("ViewSource");
        }

        private bool CanExecute() => SelectedProcess.Value != null;
        private bool CanExecuteFolder() => SelectedProcess.Value?.FilePath != null && SelectedProcess.Value != null;
        #endregion

        #region Threads Things
        private void StartWorkingThread()
        {
            _workingThread = new Thread(WorkingThreadProcess);
            _workingThread.Start();
            _metaDatesThread = new Thread(UpdateMetaDates);
            _metaDatesThread.Start();
        }

        internal void StopWorkingThread()
        {
            _tokenSource.Cancel();
            _workingThread.Join(2000);
            _workingThread.Abort();
            _workingThread = null;

            _metaDatesThread.Join(2000);
            _metaDatesThread.Abort();
            _metaDatesThread = null;
        }

        private void Synchronize()
        {
            Process[] processes = Process.GetProcesses();
            var previous = new HashSet<int>(_processesList.Keys);

            foreach (var process in processes)
            {
                _processesList.GetOrAdd(process.Id, new ProcessModel(process));
                previous.Remove(process.Id);
                if (_token.IsCancellationRequested) return;
            }

            foreach (var proc in previous)
            {
                ProcessModel p;
                _processesList.TryRemove(proc, out p);
                if (_token.IsCancellationRequested) return;
            }

            OnPropertyChanged("ViewSource");
        }

        private void WorkingThreadProcess()
        {
            int i = 0;
            while (!_token.IsCancellationRequested)
            {
                Process[] processes = Process.GetProcesses();
                var previous = new HashSet<int>(_processesList.Keys);

                foreach (var process in processes)
                {
                    _processesList.GetOrAdd(process.Id, new ProcessModel(process));
                    previous.Remove(process.Id);
                    if (_token.IsCancellationRequested) return;
                }

                foreach (var p in previous)
                {
                    ProcessModel pm;
                    _processesList.TryRemove(p, out pm);
                    if (_token.IsCancellationRequested) return;
                }

                OnPropertyChanged("ViewSource");

                for (int j = 0; j < 10; j++)
                {
                    Thread.Sleep(500);
                    if (_token.IsCancellationRequested) break;
                }
                i++;
            }
        }

        private void UpdateMetaDates()
        {
            int i = 0;
            while (!_token.IsCancellationRequested)
            {
                foreach (var process in _processesList.Values)
                {
                    process.UpdateProcess();
                    if (_token.IsCancellationRequested) return;
                }

                for (int j = 0; j < 4; j++)
                {
                    Thread.Sleep(500);
                    if (_token.IsCancellationRequested) break;
                }
                i++;
            }
        }
        #endregion

        #region OnPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
