﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DelftTools.Functions;
using DelftTools.Functions.Binding;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Networks;

namespace NetTopologySuite.Extensions.Coverages
{
    public class NetworkCoverageBindingListRow : FunctionBindingListRow
    {
        private new NetworkCoverageBindingList owner
        {
            get { return (NetworkCoverageBindingList) base.owner; }
        }

        public NetworkCoverageBindingListRow(NetworkCoverageBindingList owner) : base(owner)
        {
        }

        protected override int GetColumnIndex(string columnName)
        {
            int firstIndex = NetworkCoverageBindingList.GetIndexOfNetworkLocationArgument(owner.Function);

            if (columnName.Equals(NetworkCoverageBindingList.ColumnNameBranch))
            {
                return firstIndex;
            }
            if (columnName.Equals(NetworkCoverageBindingList.ColumnNameOffset))
            {
                return firstIndex + 1;
            }
            return BaseIndexToColumnIndex(base.GetColumnIndex(columnName));
        }

        public override object this[int columnIndex]
        {
            get
            {
                int baseIndex = ColumnIndexToBaseIndex(columnIndex);

                if (ColumnIsBranchColumn(columnIndex))
                {
                    return ((INetworkLocation) base[baseIndex]).Branch;
                }
                else if (ColumnIsOffsetColumn(columnIndex))
                {
                    return ((INetworkLocation) base[baseIndex]).Offset;
                }
                return base[baseIndex];
            }
            set
            {
                int baseIndex = ColumnIndexToBaseIndex(columnIndex);

                if (ColumnIsBranchColumn(columnIndex))
                {
                    var oldLocation = (INetworkLocation)base[baseIndex];

                    var newLocation = new NetworkLocation((IBranch)value, oldLocation.Offset);
                    base[baseIndex] = newLocation;
                }
                else if (ColumnIsOffsetColumn(columnIndex))
                {
                    var oldLocation = (INetworkLocation)base[baseIndex];
                    var newLocation = new NetworkLocation(oldLocation.Branch, Convert.ToDouble(value));
                    base[baseIndex] = newLocation;
                }
                else
                {
                    base[baseIndex] = value;
                }
            }
        }
        
        public override object this[string columnName]
        {
            get
            {
                return this[GetColumnIndex(columnName)];
            }
            set
            {
                this[GetColumnIndex(columnName)] = value;
            }
        }

        #region IndexHelpers

        private bool ColumnIsBranchColumn(int columnIndex)
        {
            int firstIndexOfNetworkLocationArgument = NetworkCoverageBindingList.GetIndexOfNetworkLocationArgument(owner.Function);

            if (columnIndex == firstIndexOfNetworkLocationArgument)
                return true;

            return false;
        }

        private bool ColumnIsOffsetColumn(int columnIndex)
        {
            int firstIndexOfNetworkLocationArgument = NetworkCoverageBindingList.GetIndexOfNetworkLocationArgument(owner.Function);

            if (columnIndex == firstIndexOfNetworkLocationArgument+1)
                return true;

            return false;
        }

        private int ColumnIndexToBaseIndex(int columnIndex)
        {
            int firstIndexOfNetworkLocationArgument = NetworkCoverageBindingList.GetIndexOfNetworkLocationArgument(owner.Function);

            if (columnIndex > firstIndexOfNetworkLocationArgument)
                return columnIndex - 1;

            return columnIndex;
        }

        private int BaseIndexToColumnIndex(int baseColumnIndex)
        {
            int firstIndexOfNetworkLocationArgument = NetworkCoverageBindingList.GetIndexOfNetworkLocationArgument(owner.Function);

            if (baseColumnIndex > firstIndexOfNetworkLocationArgument)
                return baseColumnIndex + 1;

            return baseColumnIndex;
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj);
        }

        #endregion

        public INetworkLocation GetNetworkLocation()
        {
            int indexOfNetworkLocationInBase = NetworkCoverageBindingList.GetIndexOfNetworkLocationArgument(owner.Function);

            return (INetworkLocation)base[indexOfNetworkLocationInBase];
        }
    }
}