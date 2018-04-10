using System;
using System.Collections;
using System.Collections.Specialized;
using Xamarin.Forms;

namespace BLETest
{
    public delegate void RepeaterViewItemAddedEventHandler(object sender, RepeaterViewItemAddedEventArgs args);

    public class RepeaterView : StackLayout
    {
        public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
            propertyName: "ItemsSource",
            returnType: typeof(IEnumerable),
            declaringType: typeof(RepeaterView),
            defaultValue: null,
            defaultBindingMode: BindingMode.OneWay,
            propertyChanged: ItemsChanged);

        public static readonly BindableProperty ItemTemplateProperty = BindableProperty.Create(
            propertyName: "ItemTemplate",
            returnType: typeof(DataTemplate),
            declaringType: typeof(RepeaterView),
            defaultValue: default(DataTemplate));

        public event RepeaterViewItemAddedEventHandler ItemCreated;

        public IEnumerable ItemsSource
        {
            get { return (IEnumerable)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public DataTemplate ItemTemplate
        {
            get { return (DataTemplate)GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }

        private static void ItemsChanged(BindableObject bindable, object oldValue, object newValue)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                var newValueAsEnumerable = (IEnumerable)newValue;
                var control = (RepeaterView)bindable;

                var oldObservableCollection = oldValue as INotifyCollectionChanged;

                if (oldObservableCollection != null)
                {
                    oldObservableCollection.CollectionChanged -= control.OnItemsSourceCollectionChanged;
                }

                var newObservableCollection = newValue as INotifyCollectionChanged;

                if (newObservableCollection != null)
                {
                    newObservableCollection.CollectionChanged += control.OnItemsSourceCollectionChanged;
                }

                control.Children.Clear();

                if (newValueAsEnumerable != null)
                {
                    foreach (var item in newValueAsEnumerable)
                    {
                        var view = control.CreateChildViewFor(item);
                        control.Children.Add(view);
                        control.OnItemCreated(view);
                    }
                }

                control.UpdateChildrenLayout();
                control.InvalidateLayout();
            });
        }

        protected virtual void OnItemCreated(View view) =>
            this.ItemCreated?.Invoke(this, new RepeaterViewItemAddedEventArgs(view, view.BindingContext));

        protected virtual void OnItemsSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                var invalidate = false;

                if (e.OldItems != null)
                {
                    this.Children.RemoveAt(e.OldStartingIndex);
                    invalidate = true;
                }

                if (e.NewItems != null)
                {
                    for (var i = 0; i < e.NewItems.Count; ++i)
                    {
                        var item = e.NewItems[i];
                        var view = this.CreateChildViewFor(item);

                        this.Children.Insert(i + e.NewStartingIndex, view);
                        OnItemCreated(view);
                    }

                    invalidate = true;
                }
                if (e.Action == NotifyCollectionChangedAction.Reset)
                {
                    this.Children.Clear();
                }

                if (invalidate)
                {
                    this.UpdateChildrenLayout();
                    this.InvalidateLayout();
                }
            });
        }

        private View CreateChildViewFor(object item)
        {
            this.ItemTemplate.SetValue(BindableObject.BindingContextProperty, item);
            return (View)this.ItemTemplate.CreateContent();
        }
    }

    public class RepeaterViewItemAddedEventArgs : EventArgs
    {
        private readonly View view;
        private readonly object model;

        public RepeaterViewItemAddedEventArgs(View view, object model)
        {
            this.view = view;
            this.model = model;
        }

        public View View => this.view;

        public object Model => this.model;
    }
}
