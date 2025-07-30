using SmartOrderingSystem.Helpers;
using SmartOrderingSystem.Models;
using System.Collections.Generic;

namespace SmartOrderingSystem.Repositories
{
    public class MenuRepository
    {
        private const string FilePath = "App_Data/menuitems.json";

        public List<MenuItem> GetAll()
        {
            return FileHelper.ReadMenus();
        }

        public MenuItem GetById(int id)
        {
            var menus = FileHelper.ReadMenus();
            return menus.Find(m => m.Id == id);
        }

        public void Add(MenuItem item)
        {
            var menus = FileHelper.ReadMenus();
            item.Id = menus.Count > 0 ? menus[^1].Id + 1 : 1;
            menus.Add(item);
            FileHelper.WriteMenus(menus);
        }

        public void Update(MenuItem item)
        {
            var menus = FileHelper.ReadMenus();
            var existing = menus.Find(m => m.Id == item.Id);
            if (existing != null)
            {
                existing.Name = item.Name;
                existing.Price = item.Price;
                FileHelper.WriteMenus(menus);
            }
        }

        public void Delete(int id)
        {
            var menus = FileHelper.ReadMenus();
            var toRemove = menus.Find(m => m.Id == id);
            if (toRemove != null)
            {
                menus.Remove(toRemove);
                FileHelper.WriteMenus(menus);
            }
        }
    }
}
