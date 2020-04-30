using Orthography.Enums;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Orthography.ViewModels
{
	public class MainWindowViewModel : INotifyPropertyChanged
	{
		private Person _person;
		private Number _number;
		private Mode _mode;
		private string _word1;
		private string _word2;
		private string _translation;

		public Person Person
		{
			get => _person;
			set
			{
				if (value != _person)
				{
					_person = value;
					CheckPerson();
					CheckNumber();
					RaisePropertyChanged();
					RaisePropertyChanged(@"PersonText");
				}
			}
		}

		private void CheckPerson()
		{
			var changed = true;
			var max = Mode == Mode.Imperative ? Person.Second : Person.Third;
			if (_person > max)
				_person = Person.First;
			else if (_person < Person.First)
				_person = max;
			else
				changed = false;
			if (changed)
			{
				RaisePropertyChanged(@"Person");
				RaisePropertyChanged(@"PersonText");
			}
		}

		public string PersonText
		{
			get
			{
				switch (_person)
				{
					default:
					case Person.First: return @"1st person";
					case Person.Second: return @"2nd person";
					case Person.Third: return @"3rd person";
				}
			}
		}

		public Number Number
		{
			get => _number;
			set
			{
				if (value != _number)
				{
					_number = value;
					CheckNumber();
					RaisePropertyChanged();
					RaisePropertyChanged(@"NumberText");
				}
			}
		}

		private void CheckNumber()
		{
			var changed = true;
			var min = Mode != Mode.Imperative || Mode == Mode.Imperative && Person > Person.First ? Number.Singular : Number.Plural;
			if (_number > Number.Plural)
				_number = min;
			else if (_number < min)
				_number = Number.Plural;
			else
				changed = false;
			if (changed)
			{
				RaisePropertyChanged(@"Number");
				RaisePropertyChanged(@"NumberText");
			}
		}

		public string NumberText
		{
			get
			{
				switch (_number)
				{
					default:
					case Number.Singular: return @"Singular";
					case Number.Plural: return @"Plural";
				}
			}
		}

		public Mode Mode
		{
			get => _mode;
			set
			{
				if (value != _mode)
				{
					_mode = value;
					RaisePropertyChanged();
					RaisePropertyChanged(@"ModeText");
					RaisePropertyChanged(@"Person");
					RaisePropertyChanged(@"PersonText");
					RaisePropertyChanged(@"Number");
					RaisePropertyChanged(@"NumberText");
				}
			}
		}

		public string ModeText
		{
			get
			{
				var mode = Mode.ToString();
				return int.TryParse(mode, out var _) ? string.Empty : mode;
			}
		}

		public int WordId { get; set; }

		public string Word1
		{
			get => _word1;
			set
			{
				if (value != _word1)
				{
					_word1 = value;
					RaisePropertyChanged();
				}
			}
		}

		public string Word2
		{
			get => _word2;
			set
			{
				if (value != _word2)
				{
					_word2 = value;
					RaisePropertyChanged();
				}
			}
		}

		public string Translation
		{
			get => _translation;
			set
			{
				if (value != _translation)
				{
					_translation = value;
					RaisePropertyChanged();
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public void RaisePropertyChanged([CallerMemberName] String propertyName = null)
		{
			AssertPropertyExists(propertyName);
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		protected void UpdateProperty<T>(ref T backingField, T newValue, [CallerMemberName] string propertyName = null)
		{
			if (Equals(backingField, newValue))
			{
				return;
			}

			backingField = newValue;
			RaisePropertyChanged(propertyName);
		}

		/// <summary>
		/// Warns the developer if this object does not have
		/// a public property with the specified name. This
		/// method does not exist in a Release build.
		/// </summary>
		[Conditional("DEBUG")]
		[DebuggerStepThrough]
		public virtual void AssertPropertyExists(string propertyName)
		{
			// Verify that the property name matches a real,
			// public, instance property on this object.
			var properties = TypeDescriptor.GetProperties(this);
			if (properties[propertyName] == null)
			{
				string msg = "Invalid property name: " + propertyName;
				Debug.Fail(msg);
			}
		}
	}
}
