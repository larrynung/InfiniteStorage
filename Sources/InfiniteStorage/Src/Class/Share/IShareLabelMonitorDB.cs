﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InfiniteStorage.Model;

namespace InfiniteStorage.Share
{
	public interface IShareLabelMonitorDB
	{
		ICollection<Label> QueryLabelsNeedingProcess();
	}
}
