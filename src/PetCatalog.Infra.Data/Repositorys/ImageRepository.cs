﻿using PetCatalog.Domain.Interfaces;
using PetCatalog.Domain.Models;
using PetCatalog.Infra.Data.Contexts;
using PetCatalog.Infra.Data.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;

namespace PetCatalog.Infra.Data.Repositorys
{
    public class ImageRepository : IImageRepository
    {
        private readonly IFileContext fileSaver;
        private readonly PetCatalogDbContext dbContext;
        public ImageRepository(IFileContext fileSaver, PetCatalogDbContext dbContext)
        {
            this.fileSaver = fileSaver;
            this.dbContext = dbContext;
        }
        public void Create(Image obj)
        {
            if (obj is null) throw new ArgumentNullException();
            if (obj.Data is null) throw new ArgumentNullException();

            
            var extention = Path.GetExtension(obj.Name);
            var newName = $"{Guid.NewGuid()}{extention}";
            if (fileSaver.Save(newName, obj.Data))
            {
                obj.Name = newName;
                try
                {
                    dbContext.Images.Add(obj);
                    dbContext.SaveChanges();
                }
                catch
                {
                    fileSaver.Delete(newName);
                }

            }

        }
        public Image Delete(int id)
        {
            var image = dbContext.Images.Find(id);
            if (image is null) return null;
            if (image.Name == fileSaver.DefaultFile) return null;

            try
            {
                dbContext.Images.Remove(image);
                dbContext.SaveChanges();
                if (fileSaver.Delete(image.Name))
                {
                    return image;
                }
                return image;
            }
            catch
            {
                return null;
            }         
        }
        public Image Get(int id)
        {
            return dbContext.Images.Find(id);
        }
        public IEnumerable<Image> GetAll()
        {
            return dbContext.Images;
        }
        public void Update(Image obj)
        {
            if (obj is null) throw new ArgumentNullException();
            if (obj.Data is null) throw new ArgumentNullException();


            if (obj.ImageId == dbContext.DefaultImageId)
            {
                obj.ImageId = 0;
                Create(obj);
            }
            else
            {
                var dbImage = dbContext.Images.Find(obj.ImageId);
                if (dbImage is null) throw new ArgumentNullException();
                var oldName = dbImage.Name;

                var extention = Path.GetExtension(obj.Name);
                var newName = $"{Guid.NewGuid()}{extention}";
                if (fileSaver.Update(oldName, newName, obj.Data))
                {
                    try
                    {
                        dbImage.Name = newName;
                        dbContext.SaveChanges();
                    }
                    catch
                    {

                    }

                }
            }
        }
    }
}
