using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace OwinHostedApi.api
{
    public class Person
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string LastName { get; set; }
    }

    public class ValuesController : ApiController
    {
        private ISomething some;
        //public ValuesController(ISomething something)
        //{
        //    some = something;
        //}
        public Person Get(int id)
        {
            return new Person()
            {
                Id = 4, Name = "Bob", LastName = "Steele"
            };
        }

        public string Get()
        {
            return "ok";
        }

        public void Post(Person person)
        {
            person.Name = "ok";
        }
    }
}
