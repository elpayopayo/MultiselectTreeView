using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;
using WpfMultiselectTreeViewKit.Interfaces;

namespace WpfMultiselectTreeViewKit.UserControl
{
    /// <summary>
    /// Represents an attachable behavior that applies to a TreeView, aiding in the 
    /// Binding for the read-only SelectedItem property
    /// </summary>
    public class AsyncTreeViewSelectedItemBehavior : Behavior<AsyncWpfTreeViewControl>
    {
        /// <summary>
        /// Dependency property tied to the SelectedItem
        /// </summary>
        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedItem", typeof(object),
                typeof(AsyncTreeViewSelectedItemBehavior),
                new FrameworkPropertyMetadata(null) { BindsTwoWayByDefault = true });

        /// <summary>
        /// Dependency property tied to the SelectedItems
        /// </summary>
        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.Register("SelectedItems", typeof(object),
                typeof(AsyncTreeViewSelectedItemBehavior),
                new FrameworkPropertyMetadata(null) { BindsTwoWayByDefault = true });

        /// <summary>
        /// Gets or sets the SelectedItemProperty.
        /// </summary>
        /// <remarks>This is the dependency property wrapper.</remarks>
        public object SelectedItem
        {
            get { return GetValue(SelectedItemProperty); }
            set
            {
                if (value == GetValue(SelectedItemProperty)) return;
                SetValue(SelectedItemProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets the SelectedItemProperty.
        /// </summary>
        /// <remarks>This is the dependency property wrapper.</remarks>
        public object SelectedItems
        {
            get { return GetValue(SelectedItemsProperty); }
            set
            {
                if (value == GetValue(SelectedItemsProperty)) return;
                SetValue(SelectedItemsProperty, value);
            }
        }

        /// <summary>
        /// Executes the actions that apply when the behavior gets attached.
        /// </summary>
        /// <remarks>
        /// Subscribes this instance to the SelectedItemChanged event of the attached TreeView
        /// </remarks>
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.SelectedItemChanged += OnTreeViewSelectedItemChanged;
            AssociatedObject.SelectedItemChanged += OtherBehavior;
            AssociatedObject.SelectionChanged += OnTreeViewSelectedItemsChanged;
        }

        /// <summary>
        /// Executes the actions that apply when the behavior gets detached.
        /// </summary>
        /// <remarks>
        /// Unsibscribes this instance from the SelectedItemChanged event of the attached TreeView
        /// </remarks>
        protected override void OnDetaching()
        {
            base.OnDetaching();
            if (AssociatedObject != null)
            {
                AssociatedObject.SelectedItemChanged -= OnTreeViewSelectedItemChanged;
                AssociatedObject.SelectionChanged -= OnTreeViewSelectedItemsChanged;
            }
        }

        /// <summary>
        /// Event handler for the SelectionChanged event of the attached TreeView.
        /// </summary>
        /// <param name="sender">The event sender object (the TreeView).</param>
        /// <param name="eventArgs">The event arguments.</param>
        private void OnTreeViewSelectedItemsChanged(object sender, EventArgs eventArgs)
        {
            SelectedItems = AssociatedObject.SelectedItems;
            SelectedItem = AssociatedObject.SelectedItem;
        }

        private void OnTreeViewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            SelectedItem = e.NewValue as ITreeViewNode<ITreeViewNodeValue>;
        }

        private void OtherBehavior(object sender, EventArgs e)
        {
            var newNode = AssociatedObject.SelectedItem as ITreeViewNode<ITreeViewNodeValue>;
            if (newNode == null) return;
            var behavior = this;
            var tree = behavior.AssociatedObject;

            var nodeDynasty = new List<ITreeViewNode<ITreeViewNodeValue>> { newNode };
            var parent = newNode.Parent;
            while (parent != null)
            {
                nodeDynasty.Insert(0, parent);
                parent = parent.Parent;
            }

            var currentParent = tree as ItemsControl;
            foreach (var node in nodeDynasty)
            {
                // first try the easy way
                var newParent = currentParent.ItemContainerGenerator.ContainerFromItem(node) as TreeViewItem;
                if (newParent == null)
                {
                    // if this failed, it's probably because of virtualization, and we will have to do it the hard way.
                    // this code is influenced by TreeViewItem.ExpandRecursive decompiled code, and the MSDN sample at http://code.msdn.microsoft.com/Changing-selection-in-a-6a6242c8/sourcecode?fileId=18862&pathId=753647475
                    // see also the question at http://stackoverflow.com/q/183636/46635
                    currentParent.ApplyTemplate();
                    var itemsPresenter = (ItemsPresenter)currentParent.Template.FindName("ItemsHost", currentParent);
                    if (itemsPresenter != null)
                    {
                        itemsPresenter.ApplyTemplate();
                    }
                    else
                    {
                        currentParent.UpdateLayout();
                    }

                    var virtualizingPanel = GetItemsHost(currentParent) as VirtualizingPanel;
                    CallEnsureGenerator(virtualizingPanel);
                    var index = currentParent.Items.IndexOf(node);
                    if (index < 0)
                    {
                        throw new InvalidOperationException("Node '" + node + "' cannot be fount in container");
                    }
                    CallBringIndexIntoView(virtualizingPanel, index);
                    newParent = currentParent.ItemContainerGenerator.ContainerFromIndex(index) as TreeViewItem;
                }

                if (newParent == null)
                {
                    throw new InvalidOperationException("Tree view item cannot be found or created for node '" + node + "'");
                }

                if (node == newNode)
                {
                    newParent.IsSelected = true;
                    newParent.BringIntoView();
                    break;
                }

                newParent.IsExpanded = true;
                currentParent = newParent;
            }
        }

        #region Functions to get internal members using reflection

        // Some functionality we need is hidden in internal members, so we use reflection to get them

        #region ItemsControl.ItemsHost

        static readonly PropertyInfo ItemsHostPropertyInfo = typeof(ItemsControl).GetProperty("ItemsHost", BindingFlags.Instance | BindingFlags.NonPublic);

        private static Panel GetItemsHost(ItemsControl itemsControl)
        {
            Debug.Assert(itemsControl != null);
            return ItemsHostPropertyInfo.GetValue(itemsControl, null) as Panel;
        }

        #endregion ItemsControl.ItemsHost

        #region Panel.EnsureGenerator

        private static readonly MethodInfo EnsureGeneratorMethodInfo = typeof(Panel).GetMethod("EnsureGenerator", BindingFlags.Instance | BindingFlags.NonPublic);

        private static void CallEnsureGenerator(Panel panel)
        {
            Debug.Assert(panel != null);
            EnsureGeneratorMethodInfo.Invoke(panel, null);
        }

        #endregion Panel.EnsureGenerator

        #region VirtualizingPanel.BringIndexIntoView

        private static readonly MethodInfo BringIndexIntoViewMethodInfo = typeof(VirtualizingPanel).GetMethod("BringIndexIntoView", BindingFlags.Instance | BindingFlags.NonPublic);

        private static void CallBringIndexIntoView(VirtualizingPanel virtualizingPanel, int index)
        {
            Debug.Assert(virtualizingPanel != null);
            BringIndexIntoViewMethodInfo.Invoke(virtualizingPanel, new object[] { index });
        }

        #endregion VirtualizingPanel.BringIndexIntoView

        #endregion Functions to get internal members using reflection
    }
}