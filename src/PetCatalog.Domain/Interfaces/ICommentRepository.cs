﻿using PetCatalog.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PetCatalog.Domain.Interfaces
{
    public interface ICommentRepository
    {
        public IEnumerable<Comment> GetAnimalComments (int animalId);
    }
}
