using AutoMapper;
using DuyProject.API.Configurations;
using DuyProject.API.Helpers;
using DuyProject.API.Models;
using DuyProject.API.Repositories;
using DuyProject.API.ViewModels;
using DuyProject.API.ViewModels.Pharmacy;
using MongoDB.Driver;

namespace DuyProject.API.Services;

public class PharmacyService
{
    private readonly IMongoCollection<Pharmacy> _pharmacyCollection;
    private readonly IMongoCollection<Drug> _drugCollection;
    private readonly IMongoCollection<User> _userCollection;
    private readonly IFileService _fileService;
    private readonly UserService _userService;
    private readonly GoogleMapService _googleMapService;
    private readonly IMapper _mapper;

    public PharmacyService(IMongoClient client, IMapper mapper, IFileService fileService,
    UserService userService,
    GoogleMapService googleMapService)
    {
        IMongoDatabase? database = client.GetDatabase(AppSettings.DbName);
        _pharmacyCollection = database.GetCollection<Pharmacy>(nameof(Pharmacy));
        _drugCollection = database.GetCollection<Drug>(nameof(Drug));
        _userCollection = database.GetCollection<User>(nameof(User));
        _mapper = mapper;
        _fileService = fileService;
        _userService = userService;
        _googleMapService = googleMapService;
    }

    public async Task<ServiceResult<PaginationResponse<PharmacyViewModel>>> List(int page, int pageSize, string? filterValue, string? userId = null)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 0 ? AppSettings.DefaultPageSize : pageSize;
        IQueryable<Pharmacy> query = _pharmacyCollection.AsQueryable().Where(pharmacy => !pharmacy.IsDeleted);
        UserViewModel? user = null;

        if(!string.IsNullOrEmpty(userId))
        {
            user = (await _userService.GetById(userId)).Data;
        }

        if (!string.IsNullOrEmpty(filterValue))
        {
            string lowerValue = filterValue.ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(lowerValue));
        }
        List<Pharmacy> items = query
            .OrderBy(pharmacy => pharmacy.Name)
            .Skip((page - 1) * pageSize).Take(pageSize).ToList();

        List<PharmacyViewModel> result = items.Select(pharmacy => _mapper.Map<PharmacyViewModel>(pharmacy)).ToList();
        foreach (PharmacyViewModel pharmacyView in result)
        {
            pharmacyView.Drugs = _drugCollection.AsQueryable().Where(d => pharmacyView.DrugIds.Contains(d.Id)).ToList();
            pharmacyView.Doctor = _userCollection.AsQueryable().Where(u => pharmacyView.DoctorIds.Contains(u.Id)).ToList();

            if (user != null)
            {
                var data = _googleMapService.DistanceMatrixUsingAddress(user.Address, pharmacyView.Address).Data;
                if (data != null)
                {
                    pharmacyView.Distance = data.Rows.FirstOrDefault()?.Elements.FirstOrDefault()?.Distance;
                }
            }

            try
            {
                pharmacyView.Avatar = _fileService.ReadFileAsync(pharmacyView.Id).Result.Data;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

        }

        int count = query.Count();
        var paginated = new PaginationResponse<PharmacyViewModel>
        {
            Items = result.OrderBy(x => x.Distance?.Value).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalItems = count
        };
        return new ServiceResult<PaginationResponse<PharmacyViewModel>>(paginated);
    }

    public async Task<ServiceResult<PaginationResponse<PharmacyViewModel>>> GetListViaDrugId(string drugId,int page, int pageSize, string? userId = null)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 0 ? AppSettings.DefaultPageSize : pageSize;
        IQueryable<Pharmacy> query = _pharmacyCollection.AsQueryable().Where(pharmacy => !pharmacy.IsDeleted && pharmacy.DrugIds.Contains(drugId));
        UserViewModel? user = null;

        if(!string.IsNullOrEmpty(userId))
        {
            user = (await _userService.GetById(userId)).Data;
        }

        List<Pharmacy> items = query
            .OrderBy(pharmacy => pharmacy.Name)
            .Skip((page - 1) * pageSize).Take(pageSize).ToList();

        List<PharmacyViewModel> result = items.Select(pharmacy => _mapper.Map<PharmacyViewModel>(pharmacy)).ToList();
        foreach (PharmacyViewModel pharmacyView in result)
        {
            if (user != null)
            {
                var data = _googleMapService.DistanceMatrixUsingAddress(user.Address, pharmacyView.Address).Data;
                if (data != null)
                {
                    pharmacyView.Distance = data.Rows.FirstOrDefault()?.Elements.FirstOrDefault()?.Distance;
                }
            }

            try
            {
                pharmacyView.Avatar = _fileService.ReadFileAsync(pharmacyView.Id).Result.Data;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

        }

        int count = query.Count();
        var paginated = new PaginationResponse<PharmacyViewModel>
        {
            Items = result.OrderBy(x => x.Distance?.Value).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalItems = count
        };
        return new ServiceResult<PaginationResponse<PharmacyViewModel>>(paginated);
    }
   

    public async Task<ServiceResult<PaginationResponse<PharmacyViewModel>>> AddListOfDrug(AddListDrugCommand command)
    {
        foreach( var pharmacyId in command.PharmacyIds)
        {
            var pharmacy = await _pharmacyCollection.Find(x => x.Id == pharmacyId).FirstOrDefaultAsync();
            var newDrugIds = pharmacy.DrugIds.Union(command.DrugIds).ToList();
            pharmacy.DrugIds = newDrugIds;
            await _pharmacyCollection.ReplaceOneAsync(x => x.Id == pharmacyId, pharmacy);
        }
        var page = 1;
        var pageSize = AppSettings.DefaultPageSize;
        IQueryable<Pharmacy> query = _pharmacyCollection.AsQueryable().Where(pharmacy => !pharmacy.IsDeleted);

        List<Pharmacy> items = query
            .OrderBy(pharmacy => pharmacy.Name)
            .Skip((page - 1) * pageSize).Take(pageSize).ToList();

        List<PharmacyViewModel> result = items.Select(pharmacy => _mapper.Map<PharmacyViewModel>(pharmacy)).ToList();
        foreach (PharmacyViewModel pharmacyView in result)
        {
            pharmacyView.Drugs = _drugCollection.AsQueryable().Where(d => pharmacyView.DrugIds.Contains(d.Id)).ToList();
            pharmacyView.Doctor = _userCollection.AsQueryable().Where(u => pharmacyView.DoctorIds.Contains(u.Id)).ToList();
            pharmacyView.Avatar = _fileService.ReadFileAsync(pharmacyView.Id).Result.Data;
        }

        int count = query.Count();
        var paginated = new PaginationResponse<PharmacyViewModel>
        {
            Items = result,
            Page = page,
            PageSize = pageSize,
            TotalItems = count
        };
        return new ServiceResult<PaginationResponse<PharmacyViewModel>>(paginated);
    }

    public async Task<ServiceResult<object>> UpdateCollectionFromCsv(Stream csvStream)
    {
        try
        {
            List<Pharmacy> pharmacies = new List<Pharmacy>();
            using (var reader = new StreamReader(csvStream))
            {
                reader.ReadLine();
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(',');

                    var pharmacyCommand = new PharmacyCreateCommand 
                    { 
                        Name = values[0], 
                        Address = values[1],
                        Phone = values[2], 
                        OpenTime = values[3], 
                        CloseTime = values[4], 
                        DrugIds = values[5].Split(';').ToList(), 
                        DoctorIds = values[6].Split(';').ToList() 
                    };
                    var pharmacy = _mapper.Map<PharmacyCreateCommand, Pharmacy>(pharmacyCommand);
                    pharmacies.Add(pharmacy);
                }
                await _pharmacyCollection.InsertManyAsync(pharmacies);
                return new ServiceResult<object>(pharmacies);
            }
        }
        catch (Exception ex)
        {
            return new ServiceResult<object>(ex.Message);
        }
    }

    public async Task<ServiceResult<PharmacyViewModel>> Get(string id, string? userId = null)
    {
        Pharmacy? entity = await _pharmacyCollection.Find(c => c.Id == id).FirstOrDefaultAsync();
        if (entity == null) return new ServiceResult<PharmacyViewModel>("Pharmacy was not found.");
        var pharmacy = _mapper.Map<PharmacyViewModel>(entity);
        UserViewModel? user = null;
        if(!string.IsNullOrEmpty(userId))
        {
            user = (await _userService.GetById(userId)).Data;
        }

        pharmacy.Drugs = _drugCollection.AsQueryable().Where(d => pharmacy.DrugIds.Contains(d.Id)).ToList();
        pharmacy.Doctor = _userCollection.AsQueryable().Where(u => pharmacy.DoctorIds.Contains(u.Id)).ToList();

        if (user != null)
        {
            var data = _googleMapService.DistanceMatrixUsingAddress(user.Address, pharmacy.Address).Data;
            if (data != null)
            {
                pharmacy.Distance = data.Rows.FirstOrDefault()?.Elements.FirstOrDefault()?.Distance;
            }
        }

        try
        {
            pharmacy.Avatar = _fileService.ReadFileAsync(pharmacy.Id).Result.Data;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        return new ServiceResult<PharmacyViewModel>(pharmacy);
    }

    public async Task<ServiceResult<PharmacyViewModel>> Create(PharmacyCreateCommand command)
    {
        Pharmacy? entity = _mapper.Map<PharmacyCreateCommand, Pharmacy>(command);

        bool isDrugExisted = DrugVerify(entity);

        if (!isDrugExisted)
        {
            return new ServiceResult<PharmacyViewModel>("Pharmacy contain invalid drug.");
        }
        if (!_userCollection.AsQueryable().Any(x => command.DoctorIds.Contains(x.Id)))
        {
            return new ServiceResult<PharmacyViewModel>("No register doctor found.");
        }

        await _pharmacyCollection.InsertOneAsync(entity);
        return await Get(entity.Id);
    }


    public async Task<ServiceResult<PharmacyViewModel>> Update(string id, PharmacyUpdateCommand command)
    {
        Pharmacy? entity = await _pharmacyCollection.Find(c => c.Id == id && !c.IsDeleted).FirstOrDefaultAsync();
        if (entity == null) return new ServiceResult<PharmacyViewModel>("Pharmacy was not found.");
        entity.Name = entity.Name.GetValue(command.Name);
        entity.Address.Address = entity.Address.Address.GetValue(command.Address);
        entity.Phone = entity.Phone.GetValue(command.Phone);
        entity.DrugIds = entity.DrugIds.Union(command.DrugIds).ToList();
        entity.DoctorIds = entity.DoctorIds.Union(command.DoctorIds).ToList();
        entity.OpenTime = command.OpenTime;
        entity.CloseTime = command.CloseTime;
        entity.LogoId = command.LogoId;
        entity.FollowUser = command.FollowUser;
        await _pharmacyCollection.ReplaceOneAsync(p => p.Id == id, entity);
        return await Get(id);
    }

    public async Task<ServiceResult<PharmacyViewModel>> Follow(string userName, string pharmacyId)
    {
        Pharmacy? entity = await _pharmacyCollection.Find(c => c.Id == pharmacyId && !c.IsDeleted).FirstOrDefaultAsync();
        if (entity == null) return new ServiceResult<PharmacyViewModel>("Pharmacy was not found.");
        PharmacyUpdateCommand command = _mapper.Map<Pharmacy, PharmacyUpdateCommand>(entity);
        command.FollowUser.Add(userName);
        await AddConnectBetweenUser(userName, entity);
        return await Update(pharmacyId,command);
    }

    private async Task AddConnectBetweenUser(string userName, Pharmacy pharmacy)
    {
        var userToUpdate = await _userCollection.Find(c => c.UserName == userName).FirstAsync();
        foreach (var doctorId in pharmacy.DoctorIds) 
        {
            userToUpdate.ConnectedChatUser.Add(new ConnectedChatUser
            {
                Id = doctorId,
                UserName = _userCollection.AsQueryable().FirstOrDefault(x=>x.Id == doctorId).UserName
            });

            var doctorToUpdate = await _userCollection.Find(c => c.Id == doctorId).FirstAsync();

            doctorToUpdate.ConnectedChatUser.Add(new ConnectedChatUser
            {
                Id = userToUpdate.Id,
                UserName = userToUpdate.UserName
            });

            await _userCollection.ReplaceOneAsync(t => t.Id == doctorId, doctorToUpdate);

        }
       await _userCollection.ReplaceOneAsync(t => t.UserName == userName, userToUpdate);
        
    }

    public async Task<ServiceResult<object>> ToggleActive(string id)
    {
        Pharmacy? entity = await _pharmacyCollection.Find(c => c.Id == id && !c.IsDeleted).FirstOrDefaultAsync();
        if (entity == null) return new ServiceResult<object>("Pharmacy was not found.");
        entity.IsActive = !entity.IsActive;
        await _pharmacyCollection.ReplaceOneAsync(c => c.Id == id, entity);
        return new ServiceResult<object>(new { isActive = entity.IsActive });
    }

    public async Task<ServiceResult<object>> Remove(string id)
    {
        Pharmacy? entity = await _pharmacyCollection.Find(c => c.Id == id && !c.IsDeleted).FirstOrDefaultAsync();
        if (entity == null) return new ServiceResult<object>("Pharmacy was not found.");
        entity.IsDeleted = true;
        await _pharmacyCollection.ReplaceOneAsync(c => c.Id == id, entity);
        return new ServiceResult<object>(new { isDeleted = true });
    }

    public bool DrugVerify(Pharmacy entity)
    {
        return entity.DrugIds.Select(drugId => _drugCollection.AsQueryable().Any(x => x.Id == drugId)).Any(isDrugIdValid => isDrugIdValid);
    }
}