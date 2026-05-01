using System.Windows;
using System.Windows.Controls;
using Ascon.Pilot.SDK;
using PilotMarkdownModule.ViewModels;

namespace PilotMarkdownModule.Views
{
    /// <summary>
    /// Логика взаимодействия для ObjectTreeControl.xaml
    /// </summary>
    public partial class ObjectTreeControl : UserControl
    {
        private readonly ObjectTreeViewModel _viewModel;

        public ObjectTreeControl()
            : this(null, null)
        {
        }

        public ObjectTreeControl(IObjectsRepository? objectsRepository, IEventAggregator? eventAggregator)
        {
            InitializeComponent();
            _viewModel = new ObjectTreeViewModel(objectsRepository, eventAggregator);
            DataContext = _viewModel;
        }

        private void ObjectsTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is ObjectTreeNode node)
            {
                _viewModel.SelectedNode = node;
            }
        }

        private void TreeViewItem_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (sender is TreeViewItem item && item.DataContext is ObjectTreeNode node)
            {
                _viewModel.SelectedNode = node;
            }
        }

        private void TreeViewItem_Expanded(object sender, RoutedEventArgs e)
        {
            if (sender is TreeViewItem item && item.DataContext is ObjectTreeNode node)
            {
                _viewModel.LoadChildrenCommand.Execute(node);
            }
        }

        private void AddAttributesToEditor_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.AddSelectedAttributesToEditor();
        }

        private void CopyName_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.CopySelectedName();
        }
    }
}
