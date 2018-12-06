using System;
using CarouselView.FormsPlugin.Abstractions;

namespace CarouselView.FormsPlugin.Android
{
    public interface IViewPager
    {
        void SetContentWrapEnabled(bool enabled);
        void SetPagingEnabled(bool enabled);
        void SetElement(CarouselViewControl element);
    }
}
