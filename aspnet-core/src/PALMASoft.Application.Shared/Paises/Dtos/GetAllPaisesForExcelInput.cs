using Abp.Application.Services.Dto;
using System;

namespace PALMASoft.Paises.Dtos
{
    public class GetAllPaisesForExcelInput
    {
		public string Filter { get; set; }

		public string ID_PAISFilter { get; set; }

		public string NOMBRE_PAISFilter { get; set; }



    }
}