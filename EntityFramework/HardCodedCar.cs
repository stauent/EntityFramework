﻿using System;
using System.Linq;
using DataAccessLayer.Models;

namespace EntityFramework
{
    class HardCodedCar
    {
        public static Car Add(Car NewCar, DSuiteContext db)
        {
            try
            {
                db.Cars.Add(NewCar);
                db.SaveChanges();
            }
            catch (Exception Err)
            {
            }

            return (NewCar);
        }

        public static Car Update(Car Update, DSuiteContext db)
        {
            try
            {
                Car myCar = (from c in db.Cars where c.CarId == Update.CarId select c).FirstOrDefault();
                if (myCar != null)
                {
                    db.Entry(myCar).CurrentValues.SetValues(Update);
                    db.SaveChanges();
                }
            }
            catch (Exception Err)
            {
            }

            return (Update);
        }

        public static Car Get(Car toGet, DSuiteContext db)
        {
            return (Get(toGet.CarId, db));
        }

        public static Car Get(int id, DSuiteContext db)
        {
            Car myCar = (from c in db.Cars where c.CarId == id select c).FirstOrDefault();
            return (myCar);
        }

        public static Car Delete(Car ToDelete, DSuiteContext db)
        {
            Car Deleted = null;
            if (ToDelete != null)
            {
                db.Cars.Remove(ToDelete);
                db.SaveChanges();
                Deleted = ToDelete;
            }

            return (Deleted);
        }


        public static Car Delete(int id, DSuiteContext db)
        {
            Car Deleted = null;
            try
            {
                Car ToDelete = Get(id, db);
                Deleted = Delete(ToDelete, db);
            }
            catch (Exception Err)
            {
            }

            return (Deleted);
        }

    }
}