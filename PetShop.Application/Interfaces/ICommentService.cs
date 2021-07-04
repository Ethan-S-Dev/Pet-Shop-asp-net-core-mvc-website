﻿using PetShop.Application.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PetShop.Application.Interfaces
{
    public interface ICommentService
    {
        IEnumerable<CommentViewModel> GetCommensts();
        CommentViewModel GetComment(int commentId);
        IEnumerable<CommentViewModel> GetComments(int animalId);
        void AddComment(CommentViewModel comment);
        void DeleteComment(int id);
    }
}
