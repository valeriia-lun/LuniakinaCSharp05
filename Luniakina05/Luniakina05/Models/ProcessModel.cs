using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Management;
using System.Runtime.CompilerServices;
using Microsoft.VisualBasic.Devices;


namespace Luniakina05.Models
{
    class ProcessModel : INotifyPropertyChanged
    {
        #region  Fields
        private readonly Process _process;

        private readonly string _path;
          
        private readonly DateTime _startTime;

        private float _cpu;
         
        private float _ram; 
         
        PerformanceCounter _memoryCounter;
           
        PerformanceCounter _cpuCounter;
         
        private readonly double _total;
        #endregion

        #region Constructor

        public ProcessModel(Process process)
        {
            _process = process;
            _total = (new ComputerInfo()).TotalPhysicalMemory;
            _memoryCounter = new PerformanceCounter("Process", "Working Set", Name);
            _cpuCounter = new PerformanceCounter("Process", "% Processor Time", Name);
            try
            {
                _startTime = _process.StartTime;
                _path = _process.MainModule.FileName;
            }
            catch (Exception) { }
        }
        #endregion

        #region Properties
        public int ID => _process.Id;

        public string Name => _process.ProcessName;

        public bool IsActive => _process.Responding;

        public string UserName => GetProcessOwner(ID);

        public string CPU => ((float)Math.Round(_cpu * 100f) / 100f).ToString("0.00") + "%";

        public string RAM => ((_ram / _total) * 100).ToString("0.00") + "% , " + (_ram / (1024 * 1024)).ToString("0.00") + "MB";

        public int ThreadsQuantity => _process.Threads.Count;

        public string FileInfo => _path;

        public DateTime StartTime => _startTime;

        public string FilePath => _path;

    
        public ProcessModuleCollection Modules
        {
            get
            {
                try
                {
                    ProcessModuleCollection pmc = _process.Modules;
                    return _process.Modules;
                }
                catch (Exception e)
                {
                    return null;
                }
            }
        }
   
         
        public ProcessThreadCollection Threads
        {
            get
            {
                try
                {
                    ProcessThreadCollection ptc = _process.Threads;
                    return _process.Threads;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }
        #endregion

        #region Methods
        public void StopProcess()
        {
            _process.Kill();
        }

        public string GetProcessOwner(int processId)
        {
            string query = "Select * From Win32_Process Where ProcessID = " + processId;
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
            ManagementObjectCollection processList = searcher.Get();

            foreach (ManagementObject obj in processList)
            {
                string[] argList = new string[] { string.Empty, string.Empty };
                int returnVal = Convert.ToInt32(obj.InvokeMethod("GetOwner", argList));
                if (returnVal == 0)
                {
                    return argList[1] + "\\" + argList[0];
                }
            }

            return "NO OWNER";
        }
        public void UpdateProcess()
        {
            try
            {
                _cpu = _cpuCounter.NextValue() / Environment.ProcessorCount;
                _ram = _memoryCounter.NextValue();
            }
            catch (Exception) { }
            OnPropertyChanged("RAM");
            OnPropertyChanged("CPU");
            OnPropertyChanged("ThreadsQuantity");
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
