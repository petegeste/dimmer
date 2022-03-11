using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Commons.Music.Midi;
using System.Timers;
using System.Windows;

namespace Dimmer
{
    public class MainViewModel : INotifyPropertyChanged
    {
        static readonly string CONFIG_PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), @"DimmerPresets.xml");
        private Timer ResendTimer;

        private RelayCommand<object>? _buttonClick;
        /// <summary>
        /// 
        /// </summary>
        public RelayCommand<object> ButtonClick
        {
            get => _buttonClick ??= new RelayCommand<object>(ButtonManager);
        }

        private PresetModel? _selectedPreset;
        /// <summary>
        /// 
        /// </summary>
        public PresetModel? SelectedPreset
        {
            get => _selectedPreset;
            set
            {
                if (_selectedPreset != value && value is not null)
                {
                    _selectedPreset = value;
                    RaisePropertyChanged();
                    AnimateFaders();
                }
            }
        }

        private ObservableCollection<PresetModel>? _presets;
        /// <summary>
        /// 
        /// </summary>
        public ObservableCollection<PresetModel>? Presets
        {
            get => _presets;
            set
            {
                if (_presets != value)
                {
                    _presets = value;
                    RaisePropertyChanged();
                }
            }
        }

        private IMidiOutput? _midiOutput;
        /// <summary>
        /// 
        /// </summary>
        public IMidiOutput? MidiOutput
        {
            get => _midiOutput;
            set
            {
                if (_midiOutput != value)
                {
                    _midiOutput = value;
                    RaisePropertyChanged();
                }
            }
        }

        private IMidiPortDetails? _selectedMidiPort;
        /// <summary>
        /// 
        /// </summary>
        public IMidiPortDetails? SelectedMidiPort
        {
            get => _selectedMidiPort;
            set
            {
                if (_selectedMidiPort != value)
                {
                    _selectedMidiPort = value;
                    RaisePropertyChanged();
                    //SetMidiOutput();
                    
                }
            }
        }

        private ObservableCollection<IMidiPortDetails>? _midiPorts;
        /// <summary>
        /// Midi ports
        /// </summary>
        public ObservableCollection<IMidiPortDetails>? MidiPorts
        {
            get => _midiPorts;
            set
            {
                if (_midiPorts != value)
                {
                    _midiPorts = value;
                    RaisePropertyChanged();
                }
            }
        }

        public FaderModel[]? Faders { get; set; } = new FaderModel[16];

        public byte[] FromFaders { get; set; }
        public byte[] ToFaders { get; set; }

        private double _animationProgress = 0;
        public double AnimationProgress
        {
            get => _animationProgress;
            set
            {
                _animationProgress = value;
                SendValues();
            }
        }

        public MainViewModel()
        {
            Faders = new FaderModel[16];
            for (int i = 0; i < 16; i++)
            {
                Faders[i] = new FaderModel() { Name = $"F{i+1}"};
            }
            
            RecallFromDisk();
            UpdateDeviceList();

            ResendTimer = new Timer(2.5 * 1000);
            ResendTimer.Elapsed += ResendTimer_Elapsed;
            ResendTimer.AutoReset = true;
            ResendTimer.Start();
        }

        private void ResendTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            if (Faders == null || Faders.Any(f => f is null))
            {
                return;
            }
            PostCurrentFaders();
        }

        private void ButtonManager(object par)
        {
            switch (par as string)
            {
                case "Mute":
                    MuteAll();
                    break;
                case "Send":
                    PostCurrentFaders();
                    break;
                case "Save":
                    SavePreset();
                    break;
                case "Add":
                    AddPreset();
                    break;
                case "Delete":
                    DeletePreset();
                    break;
            }
        }

        private void DeletePreset()
        {
            int index = Presets?.IndexOf(SelectedPreset) ?? -1;
            if (index < 0)
            {
                return;
            }

            int newIndex = index - 1;
            if (newIndex < 0)
            {
                newIndex = 0;
            }

            Presets.RemoveAt(index);

            if (newIndex >= Presets.Count)
            {
                AddPreset();
            }
            else
            {
                SelectedPreset = Presets[newIndex];
            }
        }

        private void AddPreset()
        {
            var preset = new PresetModel() { Name = "New Preset", Faders = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } };
            Presets?.Add(preset);
            SelectedPreset = preset;
        }

        private void SavePreset()
        {
            if (SelectedPreset == null)
            {
                return;
            }

            for (int i = 0; i < 16; i++)
            {
                SelectedPreset.Faders[i] = Faders[i].Value;
            }

            SaveToDisk();
        }

        private byte[] GetFaderValues()
        {
            return Faders?.Select(f => f.Value)?.ToArray() ?? new byte[16];
        }

        private Timer? animationTimer = null;

        private void AnimateFaders()
        {
            FromFaders = GetFaderValues();
            ToFaders = SelectedPreset.Faders;

            if (animationTimer != null) { animationTimer.Stop(); }

            AnimationProgress = 0;

            animationTimer = new Timer(100);
            animationTimer.Elapsed += AnimationTimer_Elapsed;
            animationTimer.AutoReset = true;
            animationTimer.Start();
        }

        private void AnimationTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            AnimationProgress += .1;
            if (AnimationProgress >= 1)
            {
                AnimationProgress = 1;
                SendValues();
                animationTimer?.Stop();
            }
            else
            {
                SendValues();
            }
        }

        private async Task SendValues(byte[] faderValues)
        {
            try
            {
                if (SelectedMidiPort is null)
                {
                    return;
                }

                var access = MidiAccessManager.Default;
                var output = await access.OpenOutputAsync(SelectedMidiPort.Id);

                for (int i = 0; i < 16; i++)
                {
                    Faders[i].Value = faderValues[i];
                    output?.Send(new byte[] { faderValues[i] == 0 ? MidiEvent.NoteOff : MidiEvent.NoteOn, (byte)(i + 22), faderValues[i] }, 0, 3, DateTime.Now.Ticks);
                }

                await output.CloseAsync();
            }
            catch
            {
                ;
            }

        }

        private void Access_StateChanged(object? sender, MidiConnectionEventArgs e)
        {
            //MessageBox.Show(e.ToString());
        }

        private void SendValues()
        {
            for (int i = 0; i < 16; i++)
            {
                double from = FromFaders[i];
                double to = ToFaders[i];
                double a = AnimationProgress > 1 ? 1 : AnimationProgress;
                Faders[i].Value = (byte)(from + ((to - from) * a));
            }

            PostCurrentFaders();
        }

        Task? CurrentSendTask = null;
        private void PostCurrentFaders()
        {
            if (CurrentSendTask != null && CurrentSendTask.Status == TaskStatus.Running)
            {
                return;
            }
            CurrentSendTask = SendValues(GetFaderValues());
        }

        private async void SetMidiOutput()
        {
            var access = MidiAccessManager.Default;
            MidiOutput = await access.OpenOutputAsync(SelectedMidiPort.Id);
        }

        private void UpdateDeviceList()
        {
            var access = MidiAccessManager.Default;
            MidiPorts = new ObservableCollection<IMidiPortDetails>(access.Outputs);
        }

        private void MuteAll()
        {
            foreach (var fader in Faders)
            {
                fader.Value = 0;
            }
            PostCurrentFaders();
        }

        public void SaveToDisk()
        {
            XElement file = new XElement("Settings");
            file.Add(Faders.Select(f => f.ToXElement()));
            file.Add(Presets?.Select(p => p.ToXElement()));
            file.Save(CONFIG_PATH);
        }

        public void RecallFromDisk()
        {
            if (!File.Exists(CONFIG_PATH))
            {
                Presets = new ObservableCollection<PresetModel>();
            }
            else
            {
                var file = XElement.Load(CONFIG_PATH);
                Presets = new ObservableCollection<PresetModel>(file.Elements("Preset").Select(p => PresetModel.FromXElement(p)));
                Faders = new FaderModel[16];
                int idx = 0;
                foreach (var fader in file.Elements("Fader"))
                {
                    Faders[idx++] = FaderModel.FromXElement(fader);
                }
                SelectedPreset = Presets?.FirstOrDefault();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void RaisePropertyChanged([CallerMemberName] string? prop = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }
}
