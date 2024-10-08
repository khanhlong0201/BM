﻿using System.ComponentModel.DataAnnotations;

namespace BM.Models
{
    public class BranchModel
    {
        public string? BranchId { get; set; }

        [Required(ErrorMessage = "Vui lòng điền Tên chi nhánh")]
        public string? BranchName { get; set; }

        public bool IsActive { get; set; }

        [Required(ErrorMessage = "Vui lòng điền Địa chỉ")]
        public string? Address { get; set; }

        [Required(ErrorMessage = "Vui lòng điền Số điện thoại")]
        public string? PhoneNumber { get; set; }

        public DateTime? DateCreate { get; set; }
        public int? UserCreate { get; set; }
        public DateTime? DateUpdate { get; set; }
        public int? UserUpdate { get; set; }
        public string? ListServiceType { get; set; } // loại dịch vụ
        public string? ListServiceTypeName { get; set; } // loại dịch vụ
        [Required(ErrorMessage = "Vui lòng chọn dịch vụ triển khai theo chi nhánh")]
        public List<string>? ListServiceTypes { get; set; } // danh sách nhân dịch vụ trên UI
    }
}