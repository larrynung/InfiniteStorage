﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Waveface.Model
{
	/// <summary>
	/// 
	/// </summary>
	public class Service : IService
	{
		#region Var
		private string _name;
		private IServiceSupplier _supplier;
		private ObservableCollection<IContentEntity> _observableContents;
		#endregion


		#region Private Property
		/// <summary>
		/// Gets the m_ observable contents.
		/// </summary>
		/// <value>The m_ observable contents.</value>
		private ObservableCollection<IContentEntity> m_ObservableContents
		{
			get
			{
				if (_observableContents == null)
				{
					_observableContents = new ObservableCollection<IContentEntity>();
				}
				return _observableContents;
			}
		}

		Lazy<IEnumerable<IContentEntity>> m_Contents
		{
			get;
			set;
		}
		#endregion



		#region Public Property
		public virtual string Name
		{
			get
			{
				if (_name == null)
				{
					_name = this.GetType().Name;
				}
				return _name;
			}
			private set
			{
				_name = value;
			}
		}


		/// <summary>
		/// Gets the supplier.
		/// </summary>
		/// <value>The supplier.</value>
		public IServiceSupplier Supplier
		{
			get
			{
				return _supplier;
			}
			set
			{
				_supplier = value;
			}
		}

		/// <summary>
		/// Gets or sets the contents.
		/// </summary>
		/// <value>The contents.</value>
		public IEnumerable<IContentEntity> Contents
		{
			get
			{
				return m_Contents.Value;
			}
		}

		public string Description
		{
			get;
			private set;
		}
		#endregion


		#region Constructor
		public Service()
		{

		}

		public Service(IServiceSupplier supplier, string name, IEnumerable<IContentEntity> value)
		{
			this.Supplier = supplier;
			this.Name = name;
			SetContents(value);
		}

		public Service(IServiceSupplier supplier, string name, Action<ObservableCollection<IContentEntity>> func)
		{
			this.Supplier = supplier;
			this.Name = name;
			SetContents(func);
		}
		#endregion


		#region Private Method
		private void SetContents(Action<ObservableCollection<IContentEntity>> func)
		{
			m_Contents = new Lazy<IEnumerable<IContentEntity>>(() =>
			{
				m_ObservableContents.Clear();
				func(m_ObservableContents);
				return m_ObservableContents;
			});
		}

		public void SetContents(IEnumerable<IContentEntity> values)
		{
			SetContents((contents) =>
			{
				contents.AddRange(values);
			});
		}
		#endregion


		#region Public Method
		public override string ToString()
		{
			return this.Name;
		}
		#endregion
	}
}
