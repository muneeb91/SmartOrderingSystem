using SmartOrderingSystem.Models;
using SmartOrderingSystem.Repositories;
using System.Collections.Generic;

namespace SmartOrderingSystem.Services
{
    public class MenuService
    {
        private readonly MenuRepository _repo = new MenuRepository();

        public List<MenuItem> GetAll()
        {
            return _repo.GetAll();
        }

        public MenuItem? GetById(int id)
        {
            return _repo.GetById(id);
        }

        public void Add(MenuItem item)
        {
            _repo.Add(item);
        }

        public void Update(MenuItem item)
        {
            _repo.Update(item);
        }

        public void Delete(int id)
        {
            _repo.Delete(id);
        }
    }
}
