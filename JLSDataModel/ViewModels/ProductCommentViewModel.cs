using System;
using JLSDataModel.Models.Product;
using JLSDataModel.Models.User;

namespace JLSDataModel.ViewModels;

public class ProductCommentViewModel
{
    public ProductCommentViewModel()
    {
        ProductComment = new ProductComment();
        User = new User();
    }

    public ProductComment ProductComment { get; set; }
    public User User { get; set; }

    public DateTime? CreatedOn { get; set; }

    public int? UserId { get; set; }

    public string Label { get; set; }
    public string PhotoPath { get; set; }
}