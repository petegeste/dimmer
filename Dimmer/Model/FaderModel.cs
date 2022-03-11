using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Dimmer
{
    public class FaderModel : INotifyPropertyChanged
    {
        private string? _name;
        /// <summary>
        /// Name of this fader
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

        private byte _value;
        /// <summary>
        /// Value assigned to this fader
        /// </summary>
        public byte Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = value;
                    RaisePropertyChanged();
                }
            }
        }

        public FaderModel ()
        {
            Name = "";
            Value = 0;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void RaisePropertyChanged([CallerMemberName] string? prop = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        internal XElement ToXElement()
        {
            return new XElement("Fader", new XAttribute("Name", Name ?? ""), new XAttribute("Value", Value));
        }

        internal static FaderModel FromXElement(XElement e)
        {
            return new FaderModel()
            {
                Name = e.Attribute("Name")?.Value,
                Value = Convert.ToByte(e.Attribute("Value")?.Value)
            };
        }
    }
}
