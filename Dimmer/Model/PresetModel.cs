using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace Dimmer
{
    public class PresetModel : INotifyPropertyChanged
    {
        private string? _name;
        /// <summary>
        /// Preset name
        /// </summary>
        public string? Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    RaisePropertyChanged();
                }
            }
        }

        private byte[]? _faders;
        /// <summary>
        /// Fader values
        /// </summary>
        public byte[]? Faders
        {
            get => _faders;
            set
            {
                if (_faders != value)
                {
                    _faders = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool _editingName = false;
        /// <summary>
        /// 
        /// </summary>
        public bool EditingName
        {
            get => _editingName;
            set
            {
                if (_editingName != value)
                {
                    _editingName = value;
                    RaisePropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void RaisePropertyChanged([CallerMemberName] string? prop = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        internal XElement ToXElement()
        {
            return new XElement("Preset", new XAttribute("Name", Name ?? "Unnamed"),
                new XAttribute("Values", Faders?.Select(f => f.ToString())?.Aggregate((t,s) => $"{t},{s}") ?? ""));
        }

        internal static PresetModel FromXElement(XElement p)
        {
            string[]? faderStrs = p?.Attribute("Values")?.Value?.Split(',');
            byte[] values = new byte[16];

            if (faderStrs == null || faderStrs.Length != 16)
            {
                values.Initialize();
            }
            else
            {
                for (int i = 0; i < faderStrs.Length; i++)
                {
                    values[i] = Convert.ToByte(faderStrs[i]);
                }
            }

            return new PresetModel()
            {
                Name = p?.Attribute("Name")?.Value,
                Faders = values
            };
        }
    }
}
