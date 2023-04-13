﻿using DuyProject.API.Configurations;
using DuyProject.API.Helpers;
using DuyProject.API.Models;
using DuyProject.API.Repositories;
using DuyProject.API.ViewModels;
using DuyProject.API.ViewModels.Disease;
using DuyProject.API.ViewModels.File;
using MongoDB.Driver;

namespace DuyProject.API.Services
{
    public class FileService : IFileService
    {
        private readonly string _filesPath = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "Files");
        private readonly IMongoCollection<FileDocument> _fileCollection;

        public FileService(IMongoClient mongoClient)
        {
            IMongoDatabase? database = mongoClient.GetDatabase(AppSettings.DbName);
            _fileCollection = database.GetCollection<FileDocument>("Files");
        }

        public async Task<ServiceResult<FileViewModel>> SaveFileAsync(FileCreateCommand file)
        {
            string fileExtension = FileExtensionHelper.ReturnFileExtension(file);
            var fileName = Path.GetFileNameWithoutExtension(file.RecordId) + fileExtension;
            string targetPath = Path.Combine(_filesPath, fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
            var base64Data = file.FileContent.Split(',')[1];
            var fileContent = Convert.FromBase64String(base64Data);
            await File.WriteAllBytesAsync(targetPath, fileContent);

            await _fileCollection.InsertOneAsync(new FileDocument
            {
                FilePath = targetPath,
                RecordId = file.RecordId,
            });

            var data = new FileViewModel
            {
                FileContent = base64Data,
                RecordId = file.RecordId,
                FileExtension = fileExtension,
                FileUrl = targetPath
            };

            return new ServiceResult<FileViewModel>(data);
        }


        public async Task<ServiceResult<FileViewModel>> ReadFileAsync(string recordId)
        {
            var record =  _fileCollection.AsQueryable().First(x=>x.RecordId == recordId);
            byte[] bytes = File.ReadAllBytes(record.FilePath);
            string extension = Path.GetExtension(record.FilePath);
            string base64String = Convert.ToBase64String(bytes);
            
            var data = new FileViewModel
            {
                Id = record.Id,
                FileUrl = record.FilePath,
                RecordId = recordId,
                FileContent = base64String,
                FileExtension = extension,
            };

            return new ServiceResult<FileViewModel>(data);
        }
    }
}