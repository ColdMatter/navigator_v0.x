﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using MOTMaster2.SequenceData;
using System.Dynamic;
using System.Collections.ObjectModel;
using System.Windows.Controls.Primitives;
using ErrorManager;
using DAQ.Environment;

namespace MOTMaster2
{
    /// <summary>
    /// Interaction logic for SequenceDataGrid.xaml
    /// </summary>
    public partial class SequenceDataGrid : UserControl
    {
        public SequenceDataGrid()
        {
            InitializeComponent();
            sequenceDataGrid.DataContext = new SequenceStepViewModel();
        }
        public bool IsReadOnly { get { return sequenceDataGrid.IsReadOnly; } set { sequenceDataGrid.IsReadOnly = value; } }
        public static readonly DependencyProperty RunningProperty
            = DependencyProperty.Register(
                  "IsReadOnly",
                  typeof(bool),
                  typeof(SequenceDataGrid),
                  new PropertyMetadata(false)
              );

        public void ClearSteps()
        {
            SequenceStepViewModel model = (SequenceStepViewModel)sequenceDataGrid.DataContext;
            model.SequenceSteps.Clear();
        }
        public void UpdateSequenceData()
        {
            SequenceStepViewModel model = (SequenceStepViewModel)sequenceDataGrid.DataContext;
            foreach (SequenceStep step in Controller.sequenceData.Steps) model.SequenceSteps.Add(step);
        }

        private void sequenceDataGrid_AutoGeneratedColumns(object sender, EventArgs e)
        {
            var dg = sender as DataGrid; List<string> ordNames = new List<string>();
            //These hide the columns that by default display the dictionaries corresponding to the analog and digital channel types
            dg.Columns[5].Visibility = Visibility.Collapsed;
            dg.Columns[6].Visibility = Visibility.Collapsed;

            var first = dg.ItemsSource.Cast<object>().FirstOrDefault() as SequenceStep;
            if (first == null) return;
            Dictionary<string, AnalogChannelSelector>.KeyCollection analKeys = first.AnalogValueTypes.Keys;
            foreach (var nm in Environs.Hardware.analogOrder)
            {
                if (analKeys.Contains(nm)) ordNames.Add(nm);
            }
            Style analogStyle = new Style(typeof(ComboBox));
            analogStyle.Setters.Add(new EventSetter() { Event = ComboBox.SelectionChangedEvent, Handler = new SelectionChangedEventHandler(this.sequenceDataGrid_AnalogValueChanged) });

            foreach (var name in ordNames)
            {
                string showAs = Environs.Hardware.showAnalogName(name);
                if (showAs.Equals("@")) continue;
                DataGridComboBoxColumn col = new DataGridComboBoxColumn { Header = showAs, EditingElementStyle = analogStyle };

                var resource = this.FindResource("analogProvider");
                BindingOperations.SetBinding(col, DataGridComboBoxColumn.ItemsSourceProperty, new Binding() { Source = resource });

                col.SelectedItemBinding = new Binding("AnalogValueTypes[" + name + "]");
                Style style = new Style(typeof(System.Windows.Controls.Primitives.DataGridColumnHeader));
                if (Environs.Hardware.analogColors.ContainsKey(name))
                    style.Setters.Add(new Setter
                    {
                        Property = ForegroundProperty,
                        Value = Environs.Hardware.analogColors[name]
                    });
                TransformGroup tg = new TransformGroup();
                tg.Children.Add(new RotateTransform(-90));
                tg.Children.Add(new ScaleTransform(1, -1));
                style.Setters.Add(new Setter
                {
                    Property = LayoutTransformProperty,
                    Value = tg
                });
                col.HeaderStyle = style;
                dg.Columns.Add(col);
            }

            Dictionary<string, DigitalChannelSelector>.KeyCollection digitKeys = first.DigitalValueTypes.Keys; ordNames.Clear();
            foreach (var nm in Environs.Hardware.digitalOrder)
            {
                if (digitKeys.Contains(nm)) ordNames.Add(nm);
            }
            Style digitalStyle = (Style)this.Resources["BackgroundCheckBoxStyle"];
            digitalStyle.Setters.Add(new EventSetter() { Event = CheckBox.ClickEvent, Handler = new RoutedEventHandler(this.sequenceDataGrid_chkDigitalChecked) });
            Brush[] colours = new Brush[] { new SolidColorBrush(Colors.Red), new SolidColorBrush(Colors.Orange), new SolidColorBrush(Colors.Yellow), new SolidColorBrush(Colors.Green), new SolidColorBrush(Colors.Blue) };

            foreach (var name in ordNames)
            {
                string showAs = Environs.Hardware.showDigitalName(name);
                if (showAs.Equals("@")) continue;
                DataGridCheckBoxColumn col = new DataGridCheckBoxColumn { Header = showAs };
                col.Binding = new Binding() { Path = new PropertyPath("DigitalValueTypes[" + name + "].Value") };
                col.ElementStyle = digitalStyle;

                Style style = new Style(typeof(System.Windows.Controls.Primitives.DataGridColumnHeader));
                if (Environs.Hardware.digitalColors.ContainsKey(name))
                    style.Setters.Add(new Setter
                    {
                        Property = ForegroundProperty,
                        Value = Environs.Hardware.digitalColors[name]
                    });
                TransformGroup tg = new TransformGroup();
                tg.Children.Add(new RotateTransform(-90));
                tg.Children.Add(new ScaleTransform(1, -1));
                style.Setters.Add(new Setter
                {
                    Property = LayoutTransformProperty,
                    Value = tg
                });
                col.HeaderStyle = style;
                dg.Columns.Add(col);
            }
            dg.FrozenColumnCount = 5;
        }

        //If the properties of the SequenceData are changed, this will be called
        private void sequenceDataGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            var dg = sender as DataGrid;
            if (dg.CurrentItem == null) { return; }
            ObservableCollection<SequenceStep> first = (ObservableCollection<SequenceStep>)dg.ItemsSource;
            SequenceStepViewModel model = (SequenceStepViewModel)sequenceDataGrid.DataContext;
            if (model == null) return;
            if (dg.CurrentItem.GetType() == typeof(SequenceStep)) model.SelectedSequenceStep = (SequenceStep)dg.CurrentItem;
            if (Controller.sequenceData != null) Controller.sequenceData.Steps = first;
        }

  /*      private void sequenceDataGrid_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.VerticalChange != 0)
            {
                var dg = sender as DataGrid;  if (dg.CurrentItem == null) { return; }
                //var cell = dg.CurrentItem as DataGridCell; if (cell == null) { return; }
                if (lastDataGridCellInfo == sequenceDataGrid.CurrentCell)
                {
                    if (sequenceDataGrid.CurrentColumn != null && sequenceDataGrid.CurrentColumn.GetType() == typeof(DataGridCheckBoxColumn))
                    {
                        string channelName = (string)sequenceDataGrid.CurrentColumn.Header;
                        if (channelName == "RS232Commands" || channelName == "Enabled") return;
                        SequenceStepViewModel model = (SequenceStepViewModel)sequenceDataGrid.DataContext;
                        //lastCheckBox.IsChecked = model.SelectedDigitalChannel.Value.Value;
                        ErrorMng.simpleMsg(lastCheckBoxValue.ToString() + " : " + lastCheckBox.IsChecked.ToString() + " / " + model.SelectedDigitalChannel.Value.Value.ToString());
                            //= new KeyValuePair<string, DigitalChannelSelector>(channelName, new DigitalChannelSelector(lastCheckBox.IsChecked.Value));
                    }                    
                }
            }
        }*/
        private void sequenceDataGrid_CurrentCellChanged(object sender, EventArgs e)
        {
            //Controller ctrl;
        }
        private void sequenceDataGrid_AnalogValueChanged(object sender, SelectionChangedEventArgs e)
        {
            var combo = sender as ComboBox;
            if (sequenceDataGrid.CurrentColumn == null || sequenceDataGrid.CurrentColumn.GetType() != typeof(DataGridComboBoxColumn)) return;
            string channelName1 = (string)sequenceDataGrid.CurrentColumn.Header; 
            string channelName = Environs.Hardware.nameFromAnalogShowAs(channelName1);

            if (e.AddedItems != null && e.AddedItems.Count > 0)
            {
                AnalogChannelSelector c = (AnalogChannelSelector)e.AddedItems[0];
                //if (c != AnalogChannelSelector.Continue)
                {
                    //Only raises an event if the analog channel is being changed to something other than continue
                    SequenceStepViewModel model = (SequenceStepViewModel)sequenceDataGrid.DataContext;
                    model.SelectedAnalogChannel = new KeyValuePair<string, AnalogChannelSelector>(channelName, c);

                    lastDataGridCellInfo = sequenceDataGrid.CurrentCell;
                    var cellContent = lastDataGridCellInfo.Column.GetCellContent(lastDataGridCellInfo.Item);
                    if (cellContent != null)
                    {
                        lastSelectedCell = (DataGridCell)cellContent.Parent;
                    }
                    OnChangedAnalogChannelCell(sender, e);
                }
            }
           
        }

        CheckBox lastCheckBox; bool lastCheckBoxValue;
        DataGridCellInfo lastDataGridCellInfo; public DataGridCell lastSelectedCell;
        //TODO Fix this being called when creating a new sequence step
        private void sequenceDataGrid_chkDigitalChecked(object sender, RoutedEventArgs e)
        {
            //Console.WriteLine("654654");
            Mouse.OverrideCursor = Cursors.Wait;
            try 
            { 
                var cell = sender as CheckBox; lastCheckBox = cell; lastDataGridCellInfo = sequenceDataGrid.CurrentCell; lastCheckBoxValue = cell.IsChecked.Value;
                if (sequenceDataGrid.CurrentColumn != null && sequenceDataGrid.CurrentColumn.GetType() == typeof(DataGridCheckBoxColumn) && cell != null)
                {
                    string channelName1 = (string)sequenceDataGrid.CurrentColumn.Header;
                    string channelName = Environs.Hardware.nameFromDigitalShowAs(channelName1);

                    if (channelName1 == "RS232Commands" || channelName1 == "Enabled") return;
                    SequenceStepViewModel model = (SequenceStepViewModel)sequenceDataGrid.DataContext;
                    model.SelectedDigitalChannel = new KeyValuePair<string, DigitalChannelSelector>(channelName, new DigitalChannelSelector(cell.IsChecked.Value));
                }
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
           // if (cell.IsChecked.Value) cell.Background = new SolidColorBrush(Colors.Red);
            //else cell.Background = new SolidColorBrush(Colors.Black);
        }

        public delegate void ChangedAnalogChannelCellHandler(object sender, SelectionChangedEventArgs e);
        public event ChangedAnalogChannelCellHandler ChangedAnalogChannelCell;

        protected void OnChangedAnalogChannelCell(object sender, SelectionChangedEventArgs e)
        {
            if (ChangedAnalogChannelCell != null) ChangedAnalogChannelCell(sender, e);
        }

        public delegate void ChangedRS232CellHandler(object sender, DataGridBeginningEditEventArgs e);
        public event ChangedRS232CellHandler ChangedRS232Cell;

        protected void OnChangedRS232Cell(object sender, DataGridBeginningEditEventArgs e)
        {
            if (ChangedRS232Cell != null) ChangedRS232Cell(sender, e);
        }


 /*       public static readonly RoutedEvent ChangedAnalogChannelCellEvent = EventManager.RegisterRoutedEvent("ChangedAnalogChannelCellEvent", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ComboBox));

        public static readonly RoutedEvent ChangedRS232CellEvent = EventManager.RegisterRoutedEvent("ChangedRS232CellEvent", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(CheckBox));
        
        public event RoutedEventHandler ChangedAnalogCell
        {
            add { AddHandler(ChangedAnalogChannelCellEvent, value); }
            remove { RemoveHandler(ChangedAnalogChannelCellEvent, value); }
        }
        public event RoutedEventHandler ChangedRS232Event
        {
            add { AddHandler(ChangedRS232CellEvent, value); }
            remove { RemoveHandler(ChangedRS232CellEvent, value); }
        }*/

        private void sequenceDataGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            var dg = sender as DataGrid;
            if ((string)dg.CurrentCell.Column.Header == "RS232Commands")
            {
                SequenceStepViewModel model = (SequenceStepViewModel)sequenceDataGrid.DataContext;
                model.RS232Enabled = !model.RS232Enabled;
                OnChangedRS232Cell(sender, e);
            }
            else
            { return; }

        }
        public int lastColumnIdx = 0;
        private void sequenceDataGrid_MouseMove(object sender, MouseEventArgs e)
        {
            if (ContextMenuOpened) return;
            DependencyObject dep;
            if (sender is DataGridCell)
            {
                dep = sender as DataGridCell;
            }
            else
            {
                dep = (DependencyObject)e.Source;
                while ((dep != null) && !(dep is DataGridCell))
                {  
                    dep = VisualTreeHelper.GetParent(dep); // iteratively traverse the visual tree
                }
                if (dep == null)
                    return;
            }
            if (dep is DataGridCell)
            {
                DataGridCell cell = dep as DataGridCell;

                DataGridColumn c2 = cell.Column; 
                int columnIdx = c2.DisplayIndex;
                if (lastColumnIdx != columnIdx)
                {
                    Point pCell = cell.PointToScreen(new Point(0, 0));
                    Point pGrid = this.PointFromScreen(pCell);
                    recSelector.Margin = new Thickness(0, pGrid.Y+cell.ActualHeight, 18, 0);
                    lastColumnIdx = columnIdx;
                }
                string lastDigitColName = "";
                if (c2 != null && c2.GetType() == typeof(DataGridCheckBoxColumn) && cell != null)
                {
                    string channelName1 = (string)c2.Header;
                    lastDigitColName = Environs.Hardware.nameFromDigitalShowAs(channelName1);
                }               
                SequenceStepViewModel model = (SequenceStepViewModel)sequenceDataGrid.DataContext;
                model.lastDigitColName = lastDigitColName;
            }
        }

        public DataGridCell GetCell(int row, int column)
        {
            DataGridRow rowContainer = GetRow(row);

            if (rowContainer != null)
            {
                DataGridCellsPresenter presenter = GetVisualChild<DataGridCellsPresenter>(rowContainer);

                DataGridCell cell = null;
                if (presenter != null) cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(column);
                if ((cell == null) && (presenter != null))
                {
                    sequenceDataGrid.ScrollIntoView(rowContainer, sequenceDataGrid.Columns[column]);
                    cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(column);
                }
                return cell;
            }
            return null;
        }

        public DataGridRow GetRow(int index)
        {
            DataGridRow row = (DataGridRow)sequenceDataGrid.ItemContainerGenerator.ContainerFromIndex(index);
            if (row == null)
            {
                sequenceDataGrid.UpdateLayout();
                sequenceDataGrid.ScrollIntoView(sequenceDataGrid.Items[index]);
                row = (DataGridRow)sequenceDataGrid.ItemContainerGenerator.ContainerFromIndex(index);
            }
            return row;
        }

        public static T GetVisualChild<T>(Visual parent) where T : Visual
        {
            T child = default(T);
            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < numVisuals; i++)
            {
                Visual v = (Visual)VisualTreeHelper.GetChild(parent, i);
                child = v as T;
                if (child == null)
                {
                    child = GetVisualChild<T>(v);
                }
                if (child != null)
                {
                    break;
                }
            }
            return child;
        }
        bool ContextMenuOpened = false;
        private void sequenceDataGrid_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            ContextMenuOpened = true;
        }

        private void sequenceDataGrid_ContextMenuClosing(object sender, ContextMenuEventArgs e)
        {
            ContextMenuOpened = false;
        }

        /*    private void sequenceDataGrid_MouseMove(object sender, MouseEventArgs e)
            {
                var element = (UIElement)e.Source;

                int c = Grid.GetColumn(element);
                int r = Grid.GetRow(element);

                Console.WriteLine(r.ToString() + " / " + c.ToString());
            }*/

    }

    public class ValueToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            int input;
            try
            {
                DataGridCell dgc = (DataGridCell)value;
                System.Data.DataRowView rowView = (System.Data.DataRowView)dgc.DataContext;
                input = (int)rowView.Row.ItemArray[dgc.Column.DisplayIndex];
            }
            catch (InvalidCastException e)
            {
                return DependencyProperty.UnsetValue;
            }
            switch (input)
            {
                case 1: return Brushes.Red;
                case 2: return Brushes.White;
                case 3: return Brushes.Blue;
                default: return DependencyProperty.UnsetValue;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}

