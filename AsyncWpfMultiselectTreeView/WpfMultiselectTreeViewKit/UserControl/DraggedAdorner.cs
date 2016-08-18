using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace WpfMultiselectTreeViewKit.UserControl
{
    public class DraggedAdorner : Adorner
    {
        private readonly ContentPresenter mContentPresenter;
        private double mLeft;
        private double mTop;
        private readonly AdornerLayer mAdornerLayer;

        public DraggedAdorner(object dragDropData, DragDropKeyStates dragDropKeyStates, DragTemplateSelector dragDropTemplate, UIElement adornedElement, AdornerLayer adornerLayer)
            : base(adornedElement)
        {
            mAdornerLayer = adornerLayer;

            mContentPresenter = new ContentPresenter
            {
                Content = dragDropData,
                ContentTemplate = dragDropTemplate.GetTemplate(dragDropKeyStates),
                Opacity = 0.7
            };

            mAdornerLayer.Add(this);
        }

        public void SetPosition(double left, double top)
        {
            // -1 and +13 align the dragged adorner with the dashed rectangle that shows up
            // near the mouse cursor when dragging.
            mLeft = left - 1;
            mTop = top + 13;
            if (mAdornerLayer != null)
            {
                try
                {
                    mAdornerLayer.Update(AdornedElement);
                }
                catch { }
            }
        }

        protected override Size MeasureOverride(Size constraint)
        {
            mContentPresenter.Measure(constraint);
            return mContentPresenter.DesiredSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            mContentPresenter.Arrange(new Rect(finalSize));
            return finalSize;
        }

        protected override Visual GetVisualChild(int index)
        {
            return mContentPresenter;
        }

        protected override int VisualChildrenCount
        {
            get { return 1; }
        }

        public override GeneralTransform GetDesiredTransform(GeneralTransform transform)
        {
            var desiderTransform = base.GetDesiredTransform(transform);
            if (desiderTransform == null) return null;
            GeneralTransformGroup result = new GeneralTransformGroup();
            result.Children.Add(desiderTransform);
            result.Children.Add(new TranslateTransform(mLeft, mTop));

            return result;
        }

        public void Detach()
        {
            mAdornerLayer.Remove(this);
        }

    }

    public class DragTemplateSelector 
    {
        public DataTemplate DragMoveTemplate { get; set; }
        public DataTemplate DragCopyTemplate { get; set; }
        public DataTemplate GetTemplate(DragDropKeyStates keyStates)
        {
            if ((keyStates & DragDropKeyStates.ControlKey) == DragDropKeyStates.ControlKey)
            {
                return DragCopyTemplate;
            }
            return DragMoveTemplate;
        }
    }
}
