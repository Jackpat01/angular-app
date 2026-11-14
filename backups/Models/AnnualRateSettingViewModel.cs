using System;
using System.Collections.Generic;
using System.Web.Mvc;
using Syncfusion.JavaScript.Models;
using WebPortal_OH_FI.BL.MappedObjects;

namespace WebPortal_OH_FI.Models
{
    public class AnnualRateSettingViewModel // Base for AnnualRateSetting // Parent 
    {
        public string RedSpecificInformation { get; set; }
        public List<SelectListItem> RedDateList { get; set; }
        public string RedDateSelect { get; set; }
        public DateTime? RedDate { get; set; }
        public AnnualRateSettingDetailViewModel AnnualRateSettingDetailViewModel { get; set; }
        public List<RateParameterViewModel> RateParameterViewModel { get; set; }
        public List<DatabaseLookupValueViewModel> DatabaseLookupValues { get; set; }
        public List<RatePackageViewModel> RatePackageViewModelList { get; set; }

        public List<Column> GridColumns { get; set; }

        public ProviderRateInfoDetailsViewModel providerRateInfoDetailsViewModel { get; set; }
        public List<SelectListItem> PeriodList { get; set; }
        public List<SelectListItem> MenuList { get; set; }

        public string HeaderText { get; set; }
        public string DateText { get; set; }
    }

    public class AnnualRateSettingDetailViewModel // AnnualRateSetting Details // Child
    {
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public DateTime AddREDDate { get; set; }
        public bool GetSelectedRED { get; set; }
    }

    public class RateParameterViewModel
    {
        public int RateParameterId { get; set; }
        public DateTime RateEffectiveDate { get; set; }
        public string DisplayName { get; set; }
        public string VariableName { get; set; }
        public string Value { get; set; }
    }

    public class ProviderRateInfoDetailsViewModel : IDisposable
    {
        //public string DocumentID { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public string DisplayName { get; set; }
        public int Sequence { get; set; }

        #region IDisposable Implementation
        private bool _disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects)
                }

                // free unmanaged resources (unmanaged objects) and override finalizer
                // set large fields to null
                Key = null;
                Value = null;
                DisplayName = null;

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}