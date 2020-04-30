using Orthography.Enums;
using Orthography.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Xl = Microsoft.Office.Interop.Excel;

namespace Orthography
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window, IDisposable
	{
		private MainWindowViewModel _vm { get; }
		private Random _rnd = null;
		private Xl.Application _app = null;
		private Xl.Workbook _wb = null;
		private Xl.Worksheet _dictionary = null;
		private Xl.Worksheet _statistic = null;
		private int _wordsCount = 0;

		public MainWindow()
		{
			InitializeComponent();
			_vm = (MainWindowViewModel)DataContext;
			_rnd = new Random(DateTime.Now.Millisecond);

			var values = Enum.GetValues(typeof(Mode)).OfType<Mode>().Where(p => p != Mode.Translation).ToList();
			var modes = new List<ModeComboBoxItem>
			{
				new ModeComboBoxItem
				{
					ModeId = 0,
					Name = @"All"
				}
			};
			foreach (var value in values)
				modes.Add(new ModeComboBoxItem
				{
					ModeId = (int)value,
					Name = value.ToString()
				});
			cbMode.ItemsSource = modes;
			cbMode.SelectedIndex = 0;
			_vm.Mode = Mode.Present;

			Task.Run(() => Load());

			KeyDown += MainWindow_KeyDown;
		}

		private void MainWindow_KeyDown(object sender, KeyEventArgs e)
		{
			switch(e.Key)
			{
				case Key.Enter:
					if (btnCheck.IsEnabled) btnCheck_Click(sender, e);
					e.Handled = true;
					break;
				case Key.F5:
					if (btnReset.IsEnabled) btnReset_Click(sender, e);
					e.Handled = true;
					break;
				case Key.F6:
					if (btnTranslation.IsEnabled) btnTranslation_Click(sender, e);
					e.Handled = true;
					break;
				case Key.F12:
					if (btnResetStatistic.IsEnabled) btnResetStatistic_Click(sender, e);
					e.Handled = true;
					break;
				case Key.Escape:
					if (btnAnswer.IsEnabled) btnAnswer_Click(sender, e);
					e.Handled = true;
					break;
				case Key.F1:
					if (btnPerson1.IsEnabled) btnPerson_Click(new Button { Tag = -1 }, e);
					e.Handled = true;
					break;
				case Key.F2:
					if (btnPerson2.IsEnabled) btnPerson_Click(new Button { Tag = 1 }, e);
					e.Handled = true;
					break;
				case Key.F3:
					if (btnNumber1.IsEnabled) btnNumber_Click(new Button { Tag = -1 }, e);
					e.Handled = true;
					break;
				case Key.F4:
					if (btnNumber2.IsEnabled) btnNumber_Click(new Button { Tag = 1 }, e);
					e.Handled = true;
					break;
			}
		}

		private void Load()
		{
			_app = new Xl.Application();

			var dict = $@"{Directory.GetCurrentDirectory()}\dict.xlsx";
			_wb = _app.Workbooks.Open(dict, 0, false);
			_dictionary = _wb.Worksheets[1];
			_statistic = _wb.Worksheets[2];
			_wordsCount = _dictionary.Rows.Count;
			try
			{
				_wordsCount = DictionaryCell<int>(1, 1);
			}
			catch (Exception)
			{
				MessageBox.Show(@"Failed to get Cell[1, 1] - words count.", @"Error", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
				Close();
				return;
			}

			Dispatcher.Invoke(() => GenerateWord());
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			Dispose();
		}

		public void Dispose()
		{
			_wb?.Close();

			GC.Collect();
			GC.WaitForPendingFinalizers();

			System.Runtime.InteropServices.Marshal.FinalReleaseComObject(_statistic);
			System.Runtime.InteropServices.Marshal.FinalReleaseComObject(_dictionary);
			System.Runtime.InteropServices.Marshal.FinalReleaseComObject(_wb);
			System.Runtime.InteropServices.Marshal.FinalReleaseComObject(_app);
		}

		private T DictionaryCell<T>(int row, int column) => (T)((Xl.Range)_dictionary?.Cells[row, column])?.Value2;

		private T StatisticCell<T>(int row, int column) => (T)((Xl.Range)_statistic?.Cells[row, column])?.Value2;

		private bool Write(int row, int column, dynamic value)
		{
			if (_statistic?.Cells[row, column] == null) return false;
			((Xl.Range)_statistic.Cells[row, column]).Value2 = value;
			_wb.Save();
			return true;
		}

		private bool RegisterError(string value)
		{
			var column = 1;
			while (!string.IsNullOrWhiteSpace(StatisticCell<object>(_vm.WordId, column)?.ToString())) ++column;
			return Write(_vm.WordId, column, value);
		}

		private void btnReset_Click(object sender, RoutedEventArgs e)
		{
			GenerateWord();
		}

		private void btnResetStatistic_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				_statistic.Cells.Clear();
				_wb.Save();
				MessageBox.Show(@"OK", @"Result", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
			}
			catch (Exception ex)
			{
				MessageBox.Show(@"Failed", @"Result", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
			}
		}

		private void btnCheck_Click(object sender, RoutedEventArgs e)
		{
			var word = tbInput.Text.ToLower();
			if (string.IsNullOrWhiteSpace(word)) return;
			var answer = _vm.Word2.ToLower();
			var ok = word?.Equals(answer) ?? false;
			if (ok)
			{
				MessageBox.Show(@"Correct", @"Result", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
				if (!cbLock.IsChecked.HasValue || !cbLock.IsChecked.Value)
					btnReset_Click(sender, e);
			}
			else
			{
				MessageBox.Show(@"Wrong", @"Result", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);

				var description = $"{_vm.Mode}{(int)_vm.Person + 1}{_vm.Number.ToString()[0]}_{word}_{answer}";
				RegisterError(description);
			}
			tbInput.Focus();
		}

		private void btnAnswer_Click(object sender, RoutedEventArgs e)
		{
			tbInput.Text = _vm.Word2.ToLower();
			tbInput.Focus();
		}

		private void btnTranslation_Click(object sender, RoutedEventArgs e)
		{
			if (!string.IsNullOrWhiteSpace(_vm.Translation))
			{
				_vm.Translation = string.Empty;
				return;
			}
			var translation = DictionaryCell<string>(_vm.WordId, (int)Mode.Translation);
			if (!string.IsNullOrWhiteSpace(translation))
				_vm.Translation = translation;
		}

		private void btnPerson_Click(object sender, RoutedEventArgs e)
		{
			var tag = ((Button)sender)?.Tag?.ToString();
			if (string.IsNullOrWhiteSpace(tag)) return;
			var step = int.Parse(tag);
			_vm.Person += step;
			LoadWord(false);
		}

		private void btnNumber_Click(object sender, RoutedEventArgs e)
		{
			var tag = ((Button)sender)?.Tag?.ToString();
			if (string.IsNullOrWhiteSpace(tag)) return;
			var step = int.Parse(tag);
			_vm.Number += step;
			LoadWord(false);
		}

		private void EnableControls()
		{
			tbInput.IsEnabled = true;
			btnReset.IsEnabled = true;
			btnResetStatistic.IsEnabled = true;
			btnAnswer.IsEnabled = true;
			btnTranslation.IsEnabled = true;
			btnCheck.IsEnabled = true;
			btnPerson1.IsEnabled = true;
			btnPerson2.IsEnabled = true;
			btnNumber1.IsEnabled = true;
			btnNumber2.IsEnabled = true;
			cbLock.IsEnabled = true;
		}

		private void UpdateControls()
		{
			var vis = _vm.Mode == Mode.Participle ? Visibility.Collapsed : Visibility.Visible;
			btnPerson1.Visibility = btnPerson2.Visibility = tbPerson.Visibility = vis;
			btnNumber1.Visibility = btnNumber2.Visibility = tbNumber.Visibility = vis;
			tbMode.Visibility = cbMode.SelectedIndex == 0 ? Visibility.Visible : Visibility.Collapsed;
		}

		private void GenerateWord()
		{
			var selectedMode = (ModeComboBoxItem)cbMode.SelectedItem;
			if (selectedMode.ModeId == 0)
			{
				var id =  _rnd.Next(cbMode.Items.Count - 1);
				var mode = (ModeComboBoxItem)cbMode.Items[id + 1];
				_vm.Mode = mode.Mode;
				UpdateControls();
			}
			else
				_vm.Mode = selectedMode.Mode;

			var header = 2;
#if DEBUG
			_vm.WordId = 1 + header;
#else
			_vm.WordId = _rnd.Next(_wordsCount) + 1 + header;
#endif

			LoadWord();

			EnableControls();
			tbInput.Focus();
		}

		private void LoadWord(bool update = true)
		{
			if (_vm.WordId == 0) return;

			var pairs = new List<Tuple<Person, Number>>
			{
				new Tuple<Person,Number>(Person.Second, Number.Singular),
				new Tuple<Person,Number>(Person.First, Number.Plural),
				new Tuple<Person,Number>(Person.Second, Number.Plural)
			};

			if (update)
			{
				if (_vm.Mode == Mode.Imperative)
				{
					var pair = pairs[_rnd.Next(pairs.Count)];
					_vm.Person = pair.Item1;
					_vm.Number = pair.Item2;
				}
				else if (_vm.Mode == Mode.Participle)
				{
					_vm.Person = Person.First;
					_vm.Number = Number.Singular;
				}
				else
				{
					_vm.Person = (Person)_rnd.Next(3);
					_vm.Number = (Number)_rnd.Next(2);
				}
			}

			var column = 0;
			if (_vm.Mode == Mode.Imperative)
			{
				var pair = pairs.FirstOrDefault(p => p.Item1 == _vm.Person && p.Item2 == _vm.Number);
				var imperativeId = pairs.IndexOf(pair);
				column = imperativeId + (int)_vm.Mode;
			}
			else if (_vm.Mode == Mode.Participle)
				column = (int)Mode.Participle;
			else
				column = (int)_vm.Person + (3 * (int)_vm.Number) + (int)_vm.Mode;

			_vm.Word1 = DictionaryCell<string>(_vm.WordId, 1);
			_vm.Word2 = DictionaryCell<string>(_vm.WordId, column);

			tbInput.Text = string.Empty;
			tbInput.Focus();
		}

		private void CbMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var cb = (ComboBox)sender;
			if (cb == null) return;
			var item = (ModeComboBoxItem)cb.SelectedItem;
			_vm.Mode = item.ModeId == 0 ? Mode.Present : item.Mode;

			UpdateControls();

			LoadWord();
		}
	}
}
