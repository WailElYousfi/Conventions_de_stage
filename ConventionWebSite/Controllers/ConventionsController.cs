using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Web;
using System.Web.Mvc;
using ConventionWebSite.Models;
using EO.Pdf;

namespace ConventionWebSite.Controllers
{
    [Authorize(Roles = "Student,Admin")]
    public class ConventionsController : Controller
    {
        private DataContext db = new DataContext();

        // GET: Conventions
        public ActionResult Index()
        {
            var conventions = db.conventions.Include(c => c.Employee).Include(c => c.Student);
            return View(conventions.ToList());
        }

        // GET: Conventions/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Convention convention = db.conventions.Find(id);
            if (convention == null)
            {
                return HttpNotFound();
            }
            return View(convention);
        }

        // GET: Conventions/Create
        public ActionResult Create()
        {
            ViewBag.EmployeeId = new SelectList(db.users, "UserId", "Firstname");
            ViewBag.StudentId = new SelectList(db.users, "UserId", "Firstname");
            return View();
        }

        // POST: Conventions/Create
        // Afin de déjouer les attaques par sur-validation, activez les propriétés spécifiques que vous voulez lier. Pour 
        // plus de détails, voir  https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ConventionId,CompanieName,StartDate,EndDate")] Convention convention)
        {
            if (ModelState.IsValid)
            {
                convention.RequestDate = DateTime.Now;
                convention.AcceptanceDate = null;
                convention.State = "En cours de traitement";
                User currentUser = db.users.Where(u => u.Email == HttpContext.User.Identity.Name).First();
                convention.StudentId = currentUser.UserId;
                convention.EmployeeId = null;
                db.conventions.Add(convention);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.EmployeeId = new SelectList(db.users, "UserId", "Firstname", convention.EmployeeId);
            ViewBag.StudentId = new SelectList(db.users, "UserId", "Firstname", convention.StudentId);
            return View(convention);
        }

        [Authorize(Roles = "Admin")]
        public ActionResult Approve(int? id)
        {
            Convention convention = db.conventions.Include(u => u.Employee).Where(u => u.ConventionId == id).First();
            convention.State = "Approuvée";
            User currentUser = db.users.Where(u => u.Email == HttpContext.User.Identity.Name).First();
            convention.EmployeeId = currentUser.UserId;
            convention.AcceptanceDate = DateTime.Now;
            db.Entry(convention).State = EntityState.Modified;
            string destinationEmail = convention.Student.Email;
            string destinationName = convention.Student.Lastname + " " + convention.Student.Firstname;
            string company = convention.CompanieName;

            db.SaveChanges();

            using (Stream str = new MemoryStream())
            using (var message = new MailMessage())
            using (var smtp = new SmtpClient()) {

                HtmlToPdf.Options.OutputArea = new RectangleF(0.2f, 0, 8.1f, 11f);
                //string msg = "\n\n La presente convention regle les rapports entre \n\nLa societe " + company + " et l'ecole nationale des sciences appliquees de tetouan, concernant l'etudiant " + destinationName + "\n\nLa duree de stage est de : " + convention.StartDate + " jusqu'a : " + convention.EndDate;
                //string msg = "<!DOCTYPE html><html lang=\"en\" xmlns=\"http://www.w3.org/1999/xhtml\"><head><meta charset=\"utf-8\" /><title>Convention</title></head><body>Bonjour</body></head></html>";
                string msg = "<!DOCTYPE html><html lang=\"en\" xmlns=\"http://www.w3.org/1999/xhtml\"><head><meta charset=\"utf-8\" /><title>Convention</title></head><body><br/><br/><br/><img src=\"data:image/jpeg;base64,/9j/4AAQSkZJRgABAQAAAQABAAD/2wCEAAkGBxITEhUTEhIWFRIXGBgYGBUXFxkYGBoXFxcYGhYZGiAdHSgiGB0lHhoZIjEhJykrLi4uGB8zODMtNygtLisBCgoKDg0OGxAQGy8mICYwLy0vLTUtLS0vLy8tLy0tLS0tLS01LS8tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLf/AABEIAPgAywMBEQACEQEDEQH/xAAcAAEAAgMBAQEAAAAAAAAAAAAABQYDBAcCAQj/xABNEAACAQMBAwcHCAcFBgcBAAABAgMABBESBSExBgcTIkFRYTJCcYGRobEjNFJicnOzwRQzQ3SSstEWJFOCgxdUk6LS8BVjZKPCw/FE/8QAGgEBAAMBAQEAAAAAAAAAAAAAAAIDBAEFBv/EADURAAIBAgMECAYDAAMBAQAAAAABAgMRBCExBRJBURMyM2FxgaHwFCKRscHRQlLhFSPxNHL/2gAMAwEAAhEDEQA/AO40AoBQCgFAKAUAoBQCgFAKAUAoBQCgFAKAUAoBQCgFAKAUAoBQCgFAKAUAoBQCgFAKAUAoBQCgFAKAUAoBQCgFAKAUAoBQCgFAKAUAoBQCgILY23zKqtIuGkY9GiB2PRgLlnJUDdqGWHV6wAJ7b6lHdeXDX37ZXGd9RLyqgGgg9RjlmIK6YugnmWUDHXUiFgMePdiurDzd/ed0repx1Y5e+Df4MlxyijEAuIw0qawhC+UOtpcgedpGTgccbs5FRVCW/uPIlvq10Y/7UwBQSeswmZFXraxCZM6e/KxsR2ePDPfh53+nrb9nOkRkblHCoYvqULvPVYkKIo5XZgB1QokXPHiO04rnQSdre82vwd6RcT23KK3AkJZgIywYlSAdMhibSTgNhwRu8K50E8u/9X+w6SJgHKq3DlXYL8oqRnedYaKCTVjG4fLqMb+/vxL4edrrln3Ztfgiqsb2fvT9mfaO3o4pUjIJyxDvghYwIZZck4wTiPye4g92eQouUW/bzS/JKU0nY9WO37eVkWN8l9ekY/w8avZqHtFclRnFNtafkKcXoeF24A8iuhUowRVGppX1EgME0+ScEhgWGAc6dJA70OSa/wAG/m7huUMCusbtpd3KKN+8hlQZ3bsswGPyBIdDJq6HSJOzPtryhgkClNZ1kaR0bhiGTWGwRuXTv1Hd2cd1clRlFu/Dv8gqieh5h5S2zkKsmomRYhgHym1afUdDezxGeuhNZtcLjpIsmKpJigFAKAUAoBQCgFAVDnN2ncW9sj28wicyqCxCnI0ucdYEcQPZWvBUY1am7IprzcI3RS9mc5e0Iv18UVxH2lOo/uyp9gr0auy42vB/kzRxbvZnReS/K21vlPQviQDrRP1ZF9XaPEZFeVWw86T+ZGyFSM1kT1UEzQi2NAoUKhGg5Xrv1eG5etuXcMrwOBuqx1ZPUjuo0r60soejV4hiSQRqNJYAujxgHPkppdkx5I6TGN9TjKrO7T0V/wA/Xj5EZKCyaMwvbNVKmZCInGdUuoq/SaVBLMTnX1cHt3eFc3Kjd7a93vgdvE0Y02eHTSiaQAEYECLS8dw+R1sEaDN2bg57M1NutZ3f71S+9iK3Pfn/AKZ1i2fIoGqJlP18ltKxxMGOcsMGJWByDlQc5qN6yfH3d/tnbQZ8d7EhwxRVSRgWLheuzGZyCGyOtrJO7yX7AaWq5W98PfkPkM8dtZgM66FEROp1croKKiMGYHgFiQEE4+TGeFccqjyfH3+fU7aGphjlsp3WQ6C5Z0XUR1iheJiFzhtzOuSM4YjtxXWqsFb3zOfK8zHJd28TnXFIP0fLKzuCMuGVdGqQ5YjWATwGreorqjOSyaz98heK4aHuWWwJlLugIcK7M7KQ4HSAIxPVwCW6pA8rxriVXKy96e7i8MzZYWinXqUMpc5DkHOsK+rB6w1gAg5GcdtR/wCx5HflMdvDZMyxRmPWgQqqPhlVEUJjSc40MvpDDsNdbqpbzvn7+5xbmiNz/wAJhxjSdOsPp1Np1A5GFzgDO/TjGd+Kh0kiW6jdqBIrfKvlta2PVkYvMRkQx738CexB4n1ZrRQw1Ss/lWXMrnVjDU51tHnJ2lLnokitk7MjW/rLbvdXq0tlwt8z/Bkni3wOg829/NPZCSeXpZOkkBfAG4NuG4AbuFeZjKUadVxjoaqM3OF2WispaKAUAoBQHiWJWGGUMO4gEe+uptaHGrlV29yAtJwWiX9Hl7HjGFz9ZOBHowfGtlDH1aXG6KamHhI5RtfZc9ncKsuYp160U8ZwGHep+IPfvr26dWliqdreJhcZUpHV+QXK79MQxTALdRgawODrwEieHeOw+kV4eLwroS7uBvo1lUXeW2sZcR21dixXBBlBJVWVT9AsVOtd25wVXDdnrNWQqyhp7/wjKClqRzcmMqflm1mYyK2B1Fa7FyyoMcSQoJbPk8Mbqt+Iz04W9LEOiy14/m5sS8mLdl0tqK4II1ccxSxNkjfkrM5OO0iorETTuveaf4R104vX3w/JjXk0pbU80jNrLauqCQxhYq2FwetChyAvDHDOXTu1kl7v+x0fP37sH5JWxUrjdq1DqRHG5xpOUOtcO3l6iM7jRYmad/37+gdKJuDY0emZSWxMSzcFwT9HSAAR3nJOBknFQ6V3T5Et1ZmGLk5EHRyWZ1Zm1OEYsWcyb8p1cMSRo049FSdeVmvfL3c50a1Mm0thRTEl86iUIPVOCgdQQGUqdzsN4PGuQrShp792EoKWpiuOTkT6xqdQ+chSoGlolidB1dysqrnG8YyCK6q8lb3xuHTTE/JuJi+WkAfPVyuldUiyNgFTkMwyQ2RvPhgq8lb33B00Ztn7DihKlNXVxjJzwhjhHZ9GNfXmuTqylr7zb/IjBLT3wJOqiZR+cTlkbUC2tiDduM54iJD55+sewev07sHhHWd3oUV624rLU5vyc5PzXczLF13zma4kyQCe0nizHu+Ar2a9enhYJceRipwlVkdY2JyEs4ACydPL2yS9bf4L5K+z114dbG1ar1su43QoQjwLNGgUYUAAdgGBWVtvNlyVj1XAKAUAoBQCgFARPKbYEV7A0Mo8UceUj9jD+naKto1pUp70SE4Kaszh9vNPZXOSMXNq+GA89POXxVlOR6RX0bUcVQ8TzVelOx3+xulljSVDlHUMp8GGRXzEouLaZ6id1cz1w6KAjts7ct7VdU8gTPAcWb7KjeanCnKbtFEZTUVdlTj5czXWpbGONWHkmdtTHxMcZ1hd+5hqHHOnFaXhlCzqen7ZT07llEo/KTlTtuFsTytED5JRI+jb7DgHV6NWa20cNh5r5czNUrVY6kCvLXaQOf0yX2g/EVf8HS/qV/Ez5knYc5+04z1pUlHdJGvxTSaqngKb0ViccXPiXbYPO7byELdRNAf8RT0kfrwNS+wjxrFUwM49XM0wxMXrkdEtLqOVBJE6vGwyrKQykeBHGsTTTszSnczVwCgNHbe0ltreWd/JjQsR344D1nA9dTpwc5KK4nJS3VdnB9n2s95chc5ublyzv2IvEn0Ku4DwA7a+lk44Sj4aHlpOrOx3fYmyYrWFYYVwi9vax7WY9pNfNVKkqknKWrPTjFRVkb9QJCgFAKAUAoBQCgFAKA5Nzu2Ajure4A/Wq0b+JTepPqYj1V7Wyajzh5mHGR0kWnmruS1iEP7KR4x9kHUvuYD1Vj2hBRru3Evw7vBFwrCXlT5YcrxbZihw0+N5O9Y89/e3cPWewHTQw7nm9CmrV3clqcj2ncs7NJM5ZjxZjk+j0dwHqr1qVNLJI8+c+LNOx208RIVQyEglXGpcjg2k5XPiQeFaXhlJZ6lSrWZ1rkbtCK9gaG4VJFPmtIZNQ9EgDAjGeGBuxivGxVKVCalG68rfbI9GjONSNmU7nA5vhaL+kWxLQZw6NvaPJwCD5y53b943cc7t+CxvTPcnr9zLicNuLejoUAx16TgYt4xslVygTUiW5M8pbmxk1wP1ScvE36t/SOxvrDf6RurHXw8aizNNKs4nfOSXKiC/h6SI4dcCSI+UjHv7wd+G4HHeCB4lajKk7M9KnUU1dE5VRMofPBckWkUQ/azID9lAX+IWvQ2ZDerX5IzYp2gR3M/YAtc3JG8EQp4AAM/xX2VftWo95Q8yvCRycjpteQbRQCgFAKAUAoBQCgFAKA55z0D+72x/9QPwpP6V6Wy3/wBvkZcX1Dc5pvmsv37fyR1zanb+R3C9QmuVu2/0aLCfrXyF8O9j6PiR41ko09+WehbUnuo5FfSgBnc+JJ3kk/Ek169KF8kefOVs2V4h5mzwUewf1NejGKgjI25M8zqB1V7OJ7zVsVfNlcnwLjzfbUOtYC0hI8hEY5fh8mMLle0ltagDOQcV5+PoLd31bv7u/wDyzzNmEq57r9+/E7DIkdxEynDxurI2N4I3qwB9u+vnk5U5prVZnqtKSsc/TkVZ7NSS7um/SAn6uNlAGScICMkO57zuG843ZHrPG1sVJUqa3b6v3ojD8NToJznmct2lOJZHkCLGGOQi5wN2O3eT2k9pJNezCnuRUb3POlPelc0ZEqEonVI3uT225rOdZ4T1l3MpOBIh8pG9Pf2EA9lYq9FTjus1Uari7n6P2LtSO6gjniOY5FyO8HgVPcQQQR3g14E4OEnFnqxaauilc8HkWn3rfhmvS2T2r8DLjOqvE3OaFf7ix755c/8AKPyqvaT/AO/yJ4XqF3rzzQKAUAoBQCgFAKAUAoBQHPeej5tbfvI/Clr0tl9t5GXFdQ2+aY/3Wb79v5I65tPt/I7heoV/lFfmeZ5PN4L9kcPbx9dSow3Y2K6krso20nM03Rr5Knj4jyj6uH/7XrUYqEd5mCo96VkbckWhCEUnA7Bnf44qcXd5kWssiE+NbDOb+woJnuIkt2ZZmYBWUkFc7i2RvAAySe7NVV5QjTbqaE6Sk5pQ1O9WZSztEE8iBYowpYAquEHmjJJ3DxJr5OV69ZuC1Z76tTh8z0OQcvOVpvpAqArbxnqA8Wbhrbu3bgOwZ76+iwOCWHjd9Z+7Hj4rE9K7LRFUIrcZTG61Bokma7jFZ5xLos6VzLbd0TSWbHqSgyReEijrqPSvW/yHvrxsfSy30elhan8Swc8PkWn3rfhmpbJ7V+B3F9VeJu80XzA/fS/EVTtLt2SwvZl2rAaRQCgFAKAUAoBQCgFAKA57z0fNrb95H4UtelsvtvIy4rqGtyJuNGzbojiZmUf5ljX881PHxviURoO1Jle2vcdHE79oG70ncPeauox3pJFNSVk2afJvZwji6VwNTgMMjIVPMJHnM28heGN53VqrTcpbq4e/Tn9CinDdV2fNqbVcsscas5OcKXlz4aViZQO3cBVtKjGzlJ28l+bkZ1ZXSX5/FjXnvSxKXMbK2M6JiwGPqO46SE7t2Syk8RiktylHfUlbnl6pZP0fIK83uyjny/XFfYl+TG1obBHmHXZgTFuw0gIAVGO8LpYNqxw0edrXGevTeM3VF5cc8l3+fD8WZbTksMm3rw5+/fEqc20ZHkklZ8ySa9bfbGGA7gQcbuzdwrfRdGUFGm00vwZKnSRleeTZqFxUHjsOpbrmvffodWGqtX3Wfa1LMpPBNURr06jcYyTaLJUpwSclYwyCuSR2LNjYm0Db3EE4OOilRj9jOHHrUkeusNeG9Fx5mqjK0kzrnPD5Np96/wCGaxbJ7V+BsxfVRv8ANF8wP30vxFU7S7dksL2ZdqwGkUAoBQCgFAKAUAoBQCgOe89Hza2/eR+FLXpbL7byMuK6hEcnWxs6Qd93j/2gfyq7Gf8A0+RXS7LzILlKupY4846SVFz4HP54q/DZNy5IorK9lzZJbUcdUAYXe2B9oqB6lVRUqKOVGS11sueGGOO2xFJIM3F2xC9Gg80Md43k8O7szkUwrU5zcqmaXVjzLnTlCKUMm9XyKpzhbaiuJIkibWsKlelPF2OMnxG7j2kns3muvQlSwjcsrtZctTtOtGddKPBPP6Fbd3VEVvIb5RfDJKMR3AlMEfVB9PcHUeErKE+rK2f0f5z+pzEw6eneOqv+vwYk7f8AvsqezYOeGqQTs3l6EMZJRrQk+H7PO8cRke+qKbjh49HiaXn/AL+mWyUqr36NTy9/o9NIAM5AHjur1cTiY0sNvw4q0ffcYKNGU627Lhm/feak95GuCXX1HPw/73V41Pdws6dSMk7r5s9Pa9UelO9eM4NW5GaSvopHkI1pR1WrJU1NEGdf5xptdps1zxbDe2EGvP2YrV5L3qbsV1ETfNF8wP30vxFZ9pduyeF7Mu1YDSKAUAoBQCgFAKAUAoBQHPeej5tbfvI/Clr0tl9t5GXFdQiOTqZ2dIe67z/7QH51djP/AKfIrpdl5kBynOBCe6Vfgf6Vow3HwKK3DxJParjcw3DrMD2aSdWPSrFgfQK7R5HKnM2eUF7s+9RJJLpoimcoAWzniAMbz3MO/wBnMPTxGHbjGF78ff2JVZ0ayTlK1ih8qY1uwsNpEsaLuXVjW2DlnlYe3uUA1bicNUnQalLNtPu8F7zK6NeEaqcVl6+LNzbWxnltkjiikbhokWN86I1IB3DcJGaR9PHGk91V1KEKkOjnJJpc1r/iSX1LI1JQkpRi2n3cP/cyv7P2ZOsLjJLBsawGYKWwFByOJwd1TwuGnRpShvJSeaK69aNSopbraWprTW1y6lHaIKeJAbO493D4VGeHxtWHRzlG3F+1+jsauGpy34p3Pc2zNTRKSOhjHA5yT49nd76nPAOUqcL/ACR+rZGOLUVOX8n6C72TEyEKiq3YfH+lTr7PpSptQik+DI0sZUjNOTujLboyoocgsBgkeHD3YqdCE4UlGeqI1ZRlNyjozHMcKxqupqTgjsXOVB0dts5PonT/AAw4rztlu9aTN2K6iJnmi+YH76X4iqNpduyeF7Mu1YDSKAUAoBQCgFAKAUAoBQHPeej5tbfvI/Clr0tl9t5GXF9Q1+Q9t0mzbtRvPSswHiqRsB68Y9dWbQe7iEyOHV6bKrykTVAxzwwwPoP9M1dhpWmiisvlZH2u0ZZj5OIM58oqwYeejDeH9RHDIOBWzcjFd/vJrkZ1OTfce7+KBcHqE7/LSRCT4dC2n14FWU3N5Z/VP75kZ7muX0f4NGHaKhSOjHHyBuQ435fJLyb/ADS2ncOPCrnSbevnx8uC8bXK1VSWn68+LM+z9sMq3Ot3LSxaVIPn9JG2ePV6qkbvAVCrh1JwslZP0syUKzSld6r1JuDlLbJHbBIwDG8DMvRKWDIR07q2cEuAd537xwwCMksJVlOd3qmteeit3e7mhYmnGMbLS3D6s8JtuzyxYa8vKzj9Gj+XV1AiAOcw6D3cfK3kmuvD17JLLJW+Z/LbXxv/AJoOno53+2vLwt/phh2nY6F1K2oraKyiGM6TBumYFiQ2vjvG/tqUqOI3nZ5fNxf8tPp/4RVSjuq/dw5a/UybQ5QWgLvDGusxRKGMEeNazsZGwQQCYiBnGTjsquGGrWSm8rv+T0tl6k5V6d24rguHf+iqbenie4laBdMLOxRcYwpOQMdnorRT3o00p62zM891zbjofeTWzTc3dtBjIeVdQ+ovXk/5FasmInuwbNNGN5JHV+eHybT71/wzWTZPavwNWL6qN/mi+YH76X4iqdpduyWF7Mu1YDSKAUAoBQCgFAKAUAoBQHPuecf3a3/eV/Clr0tl9t5GbF9Qz80nzef78/hpXdq9svA5hOoVHlrsRlu+hIxbD5VT9MEkBB9g5B8NPeKnhqq3N7joVVqeduBD2fU1RHzTlfFGOR7Dkeqtt7/MZrWyMG0k1Ke8bx6q00pWZTNXRCg1rM59roFAKA+E1xsGJ2quTJpGuxyazzkXRR1TmT2ASZL5xuIMUPiM/KuPWAoPg9eLjqukF4s9LDQst4lOeLybT71/5Ks2T2r8Bi+qiR5o/mH+tL/NVO0e3ZLC9mXWsBpFAKAUAoBQCgFAKAUAoDn3PN82t/3lfwpa9HZfbeRmxXZmfmk+bz/fn8NKltXtl4HMJ1WWDlXsIXUOFIWZMtGx4ZxvVvqtwPqPECsNKpuS7i+pDeRxq/jbUQVKTRkhkbcQfORvAjBBG7gRkV7FOa8mefOLNYzZGfd2g9xrTFlEkRNymk+B/wC8VshO6M8o2MWqrLkLDVS4sfC1cudseGeoORJIwu9UykTUSZ5H8mZb+cRJlY1wZpfoL3D6x3gD0ngDWHEYhU1f6GujS3mfo2ws0hjSKJQsaKFVR2ADArwpScndnppWyKBzxeTafev/ACV6mye0fgZcX1USPNH8w/1pf5qo2j27JYXsy61hNIoBQCgFAKAUAoBQCgFAc+55vm1v+8r+FLXpbL7byM2K7Mz80nzef78/hpXdq9svA5hOqy9V5hqKzyv5IR3g1qeiuVGFkxkMPoSDzl7u0Z3doN9Gu6eXArqU1PxOO7bsJraTRcRmJzwPGOQDtRuDe5h2gcK9alVjJXizBUpuLzIuZ87jWqMzPKJpM2KvUytxPPSV3fObp5MlRczqieGeq5TJqJaOR3IW5viGAMVt2zsPKHdEPPPjwHecYrDXxcYZavl+zXSw7lrod32DsSCzhWGBNKDeTxZm7WY+cx7/AFcABXjzqSm7yN8YqKsiRqBI5zzxeTafev8AyV6uye0fgZMX1USPNH8w/wBaX+aqNo9uyWF7MutYTSKAUAoBQCgFAKAUAoBQHPueb5tb/vK/hS16Wy+28jPiuzM/NGf7vP8Afn8NK7tXtl4EcJ1WXqvMNQoDBe2UcyGOWNZIzxV1DA+o12MnF3RxpPUoO2eaW2fJtppLc/RPysfqDEMP4vVWyGOmusrlEsNF6ZFUveaTaAPUkt5B9p0PsKke+tUcfDimUPCPmaS81W08+TEPEy7vcKl8fT7yPwkiTsOZy6b9dcQxj6geU+/QPjVctoR4JlkcJzZdtgc2dhbEOyG4kHBpsMoPggAX2gnxrHUxdSeWhfChCJcwKzFx9oBQHOeeLhZ/eP8AyV6uye0fgZMX1USPNH8w/wBaX+aqNo9uyWF7MutYTSKAUAoBQCgFAKAUAoBQHPuef5tb/vK/hS16Wy+28jPiuzMvNF+ouPv/AP60ru1e2XgQwnVZfK8w1igFAKAUAoBQCgFAKAUBznni4Wf3kn8lersntH4GTF9VEjzR/MP9aX+aqdo9uyWF7MutYDSKAUAoBQCgFAKAUAoBQFR5ydgz3cEKQKGZJg7AsF6oRx2+LCtmCrRo1N6RTXg5xsj3ze7DmtYpVnABeXUMMG3aFHZ4g13H14VqilHkcoU3CNmWa5uEjUvIyog4sxAA9JPCsaTeSL72KPtnnY2fDkRF7hh/hjC/xNgH1ZrXDBVZaqxRLEQRV7rnqlz8nZoB9eQk+5RWhbN5yK3iu41hz0Xf+6wfxPUv+OjzHxXcb1nz1tn5WzGO9JPyZfzqD2a+EgsVzRZdl87GzpcB2kgP/mJ1f4lyB68VnlgqsdFctjXiy5WG0IZl1wypIvejBh7uFZpRcdUWpp6GzUTooBQCgKdzi8np7sW/QBT0buWy2ncVwMd++t2AxEKM25GfEU3NJI3Ob/Y8tra9FMAH6R23HUMMd2+q8XVjVq70SdGDhGzLLWUtFAKAUAoBQCgFAcu55ds31s1ubeV4oGDBmTGTJncGOOGngPTW/A0qdS6lqZ8ROUVdEDya53riLCXidOn+IuFkHpHkv7jV9XZ6ecMvErhif7F9sec/Zcg3zmM90iMvvAI99Y5YOsuFy9VoMyXXOXstBn9KD+CI7H3LXFhKz/iddaC4lU23zzoARaW7MfpzHSB46VJJ9orRT2e31mUyxK4I5rtvb93fyDppHlYnqRKDpB+oi9vjvPjXoU6VOktPMzSqSqMsWw+avaE4DSBbdD/iHL4+wv5kVTUx9OOSzLY4eT1LhZcy9uP1t1M5+oEQe8NWSW0aj0Vi1YaPEkRzQbO75z/qD/pqv4+tz9CXw8DXuOZuxPkTXCH7SMPen51JbQqrU48NHgQW0uZeUb7e7VvqyIV96k/CrobR/siEsLyZVLzkZtWxbpFilXH7S3Yt/J1sekVoWIoVdfUqdOpDQ3dlc6m0YDpkZJgNxWVcP7Vwc+kGozwNKSvHIkq8o6lv2dz0QnAntZEPaY2Vx7DpNZp7OmuqyxYqPEsNpzo7LfjOUPc8bj34I99Z5YOtHgWqtB8SSTlxs08L2D1uB8ah8PV/qyXSQ5h+XGzRxvYP4wfhT4er/VjpIcyNu+dHZacLgue5I3b34x76sjg6z4EXWguJo2nOJNeNo2dYSS78dLMwjiX0kZ9gOa7LDKnnOX7ORq73VRcNkW9wo1XMyvIfNjXREvgucs3pY+gCs0rX+UtV+JI1E6KAUAoBQCgNXadhFPE0U6K8TDrK3D0+B8alGTi7rU40mrM4/wAqOaCZCXsXEif4TkK48A3BvXg+mvTo7QWlRGSeG4xKJd8mb2I4ktJwfCNmHtUEVtjiKUldSKHSmuB9tOTN9KcR2k5PjGyj2sAKPEUo5thUpN6Fw2FzQ3kpBuXS3TuBEknu6o9p9FZam0ILqF0cM+J1bkxyQtLFcQR9c+VK/Wkb19g8BgV5tWvOq7yZqjTjHQ2ds8o7W1Hy8yqexeLn0KN9do4WrWdqcbnJ1YQ6zKbtDnYiBIht3fuZ2CD2DJr1aewqrV5yS9TNLGrgiIk52LnzbeEDxLn8xWqOwqfGTKvjpcgnOxddtvCfQXH5mktg0+EmPjpciSs+dpP2tqw8UcN7iBWeewqi6s0yxY1cUWTZnL+wmIHTdGx82UFPf5PvrBV2biaWbj9M/sXxxNOXElr3ZFpcrmWGKYHziqt7DWNSnB8i20ZFavuarZkmSsTxH/y5GA9jZHurRHG1lxIOjB8CGuOZe2PkXUy/aCN8AKtW0ai4Ir+Gia3+xNP99b/hL/1VL/kp8h8MuZlh5lYPOu5T9lEX45rn/Iz5IfDRLBsjmv2bAQTE0zDtmbUP4QAvuqieLqy42JxoQRcYYlRQqKFUbgqgAAeAHCszdy491wCgFAKAUAoBQHxhncd4oCt7RvJ7Hr9G1xZ9unfND7f1kfvHiOGqlThW+W9pej/0qnJwztdG3svlZZXABjuI8/RY6GHqbBrlXCVqXWizsa0JaMkWv4gMmVAO8uv9azqLZPeXMhdqcubCHOZ1dh5sfXPu3D1mtlLZ+Iq6RZVLEU48Sg8oOcq4n+TtUMKndkdaVvRjcvqyfGvZw2x6dNb9Z3tw4GKpi5SyjkauyObm9uD0kxEIbeTIS0h8ccc+kirKu16FHKmr+GSORwlSecmW6y5q7Rf1sksh9IQe4Z99eZU2zXk/lsvU0xwcFqSK83Ozh+xb1ySf9VU/8riv7eiJ/C0uRjm5tdnHhG6+Ilf8yRXVtfFL+XojnwlLl6kPfc00Rz0Ny6nudVYe0aa1U9uVF14p+hXLBR4Mqu1ebu/hyVjEy98Zyf4Tg+zNelR2xQnk/lM08JOOhX7e8uLZsJJLA/aoLJ7VPH2VsdGhiFfdT7/9Kd6cHrYsdjzkbQjwGdJR9dBn2risVTY2Hn1br1+5fHF1V3k1bc7T/tLVT4pIR7iv51knsH+s/QuWNfFG/HztQ9ttKPQyn+lZ3sOsv5L1JLHQ5H1udmDstpT60H50Ww67/kvX9HfjYcjSuedtv2doP88n5BavhsF/yn6Fbx3KJCXnOXfv5JjjH1UyfaxNa6excPHVt+hXLF1H3EXBtTaV5II0nnkc8ArlQPE6cBR4mr54fB4WO9KK88/uVxnVqSybO67HtnigijkcyOqKrOTkswG857a+PqyUptpWR60VZWZuVAkKAUAoBQHwnG88KAxW93HJno3V8cdLBvhXXFrU5dMrHKDm9s7klwphkPFo8AE95U7j6sGt+G2nXoLdTuuTKKuGhPuKpNzRyZ6t0hH1ozn3NXpx27G3zQM7wUuEjbsOaVR+uuSR3RoF97E/Cqqm3JvKEbHY4FLVlssdiWGzkMoVIgo3zSHLfxNw9ArzK2Jr4l2k79xqhShTWRUNv88cCEraQmY/4j9RPUPKPsFW0tnylnJ2K54lLqlIv+dHachOJliHdHGu71tk1sjgaUdcyl4iTIp+XO0id99N/EB8BViwtH+qI9NPmZYOcDaa8L2Q/a0N8VrjwlF/xHTT5k9s/nfv0/WLDMPFSh9qnHuqiWz6fBtE1iZcS4bG54bOTAuI5ID9L9YntXrD2Vmns+ourmXRxEXqXGKaxv06pguU/wAr4/NayrpKTurplr3ZrmQe0ebOxkyUDwn6jZHsbNb6W18TDJu/j/liiWDpvTIr13zSv+yulPg6EfA1up7ey+eH0/0peB5MipubC/HAwt6HI+K1ohtyjxTRW8FMw/7Nto/4cf8AxB/SrHtrD+0R+EqGzb81183lNCg+0zfBaqltygtIsksHUerLHsrmpgXBuJnk+qg0L7d5PtFYa2260laCS+5fDBQWbdy77M2XBbpphjWNe3SMZ8SeJ9deRVrTqvem7s1xioqyN2qyQoBQCgFAKAiuVOzGubSeBG0NJGyhvHsz4HgfAmp05bs1JkZK6sfmgC5spyAXt7iM4OCVI/JlPrBr6C1OtG+qPOvODL1sfniu4wFuIY5wPOBMb+vAKn2CsdTZ0H1WXRxNtScPPXFj5nJn7xMfD8qp/wCOnzLPiVyIXanPHdvkQQxQ/WYmRvVwHuNXQ2dFdZlcsTLgij320bu9lXpJJbiUnqpvbf8AVUbh6hWuMKdKOlilynNl55O80FxKA93IIFPmLh5PWfJX31kqbQSygi6OGb6xftmc2WzIgMwdK30pWLH2bl91YZYytLiaFRguBOxcnLJRhbS3HohT+lVOrUf8n9Se7Hken5P2Z42kB9MSf0p0s/7P6jdjyIi+5u9mS5zaIpPbGWjPq0kVOOKqx0kyLpQfAqm1uZiE5Ntcuh7FkAdfRkYI99aYbQmusrlUsNF6FM2hzabUt21xx9Jjg8D9b04OlvZmtaxlGas/UpdCpHQxwctNsWZ0ySygDdpuI9XvYavfT4fD1M1byY6WrHJk9Zc9FyP1ttDJ4ozJ8dVVy2bHhL0JrFPiiYg56ofPs5QfqujfHFUvZ0+DJrFR5Gf/AG0Wn+7T/wDJ/wBVR/46rzQ+KjyNS657Ix+rs3P25FX4A1OOzZcZB4lcEZtn8rNuX3zWzigjP7WQMQB4FsavUpqEqOHp9aV33HVUqS0Vi3bG5LupEl7cyXc3EBurCh+pGOqSPpNk92KyzmnlFWRdGLWpZaqJCgFAKAUAoBQEJym5K2t8um4jyw8mReq6+g93gciraVaVN3iRlBSVmcx2tzMzgk21yjr2LKCje1cg+wV6ENoL+cTLLDPgyKTmj2kTg9AB39IT/wDCrXtCnwuR+GlzLDsjmX3g3V1u7UhXH/M39Kzz2i/4osjhVxZ0Xk/yYtLNcW8KoTxfi7eljvPo4VhqVZ1OszRGCjofV5R25QPltJn/AEYdU75RIY8Dw1A7+4Vzo5aeZ3eRL1A6YL68jhjaWVgsaDLMewfEnwHGupNuyBCbG5WJcTLEIJow6s8bSpo1qmNZCk6gBqXiBnNWSpOKvcgppuxI2e2oZLc3Kk9CBISSN+IywY44+aag4tOxJNNXPlttuF2hQE654jMgI/ZjRknsB667vTTdefcN5ElUTp5kjDDDAEdxGaAiLvkpYSeXZwE9/RqD7QKsjVnHRkXCL4ENtPkXsWFDLPBDFGOLu7Iozw84CrPia39iPRQ5GxHzebK4izQ+tz8Wp8VW/sOihyJPZ/JeyhOYrSFG+kI1z7cZquVWctWSUIrRG3ZbSjlkmjQnVCyo+7A1MgcAd+4iouLST5nUzcqJ0UAoBQCgFAKA8yPgEngAT7KAgdrX8/6CJoFMruqN8mUDCNyC5QsQuQhOM9wqcIpysyLbtkY+RVjbLD+kwRyRC4VWZZXZz1dWGOWbeQck537q7VbvZ8BFJZm5Yco4JZZY0dfk9PW1DD5LBiveFKlSRuyCOyuODSuFJN2NPk5yygu+nwrQiB9DGUoueO8YY7t3b31OpQlTtficjUUibXaEJOBLGT3a1z2ePiPbVVmSujnyDNrCRvDbXYgjtH6VJvHsNa5P53/+V9kVLq+f5LhyivypjhVtLyiQr1ghcxhT0asQdBbVxwSArY37xmjG6bLJPgVi4YQwWlvLMHnN5HIydK02hWmZgpdt5VchQzYzir1m20srEJZK1yaupVbalrpIbFvck4IOAWhAziqo5U3c6+uiJ2RexR7EfXIi/JXI3sBvLy4HpJNWVE3VVu77I5FpQMuyCv6Zs7QSyDZ0mGKlSQGtgCQd4z3GuS6sr8zq1RbtpbQjgQySsFUbh3sx3BVHnMTuAHHNUxi5OyJtpamrY7egkgimMiJ0gQ6S4JDPjqeJBOPVRwknaw3kQ20uVwS+S2ElukWlGdpHOtzIXVVhxuLBlGQd51irVRfRudn9CG/81jLy426tusaCESyvrZNcbuimMZydKk6jnA4dpyAKjSpud8zs5WMW0+XMVvbWs0kbO04jysRU6WdQd+phgZOB6KlHDylJpcDkqiilclNs8p7a2MQkfLTSLGiINbFm8B2eNQjTlK9uBJySI3k1Oi3e0tTKvy8fEgf/AM8XfU6mcIW7/uRi0mz7snlcJL1rZ+iAdNduUk1s6gnUHA8hsAMB3GkqDUN86pXdi1VQTFAKAUAoBQAigKJc8i40uI0jgnktHYtJG10y20e/OBFv1j6vk1p6eUo5tX8MyG7mWna2wre5UJMhZF4KHdF9YVgD66pjOUXdEmkzBeclLGVY0ktIXWJdMYKKQi/RHcK6qs0209RZGGbkTs1sarG3OAFHyajAHAbhXVXqJWTObqMf9g9l/wC4W/8AwxXfiKnMbqJq22fDGiRpGixx40KFGFxkAqOw7z7TVTk27slYxbX2Pb3SCO4iWVAQwDDOGHAjtB9FdjJxd0caT1Nb+zFl0Jg/RYuhJDFNA0lhwJ7z4mu9JLe3r5nN1HvZHJ20tSzW1vHEzABiigEgcAaSqSl1mdSSNRORWzQ/SCyg151ajGp35znf41Lp6lrXG6icEK6tWkasac436eOM93hVR0jDybtTOLlotU4JKu7O+kntUMxVfUBip78t3d4HLLUjNr8j7MkOmzbaV5JV6UsFTCsTrk4dYjjjtzU41prK5xxRJbN5MWVudUFpDG30ljUN7cZqMqs5as7uq9yVdAQQRkEYI7weIqs6QMPIjZq502NuNQwfk14Hs4Va69R6s5uo2tl8mLK3bXBawxv9JY1De3Ga5KrOWrG6jDtHkhYTu0s1pE8jcXZAWOBgb/RXY1pxVkzjime9jbFSCWTRb20UXV6IxRhZMY+U1nHfwx66jKV1qwopEzUCQoBQCgFAKAieVl28VnPJG2l0QkNuOMdu8Yq7DxUqkUyFRtRbRH7W25JBcXOB0iRW1u6xZCjXJPOhOrBIyFXju6vpqynSjOEeF28/BI5KTVzyNvys6I6dHIl4tvII3Dowa3Mw3tGCVwy5GFORxxx70MUm07rduvrbn+zjm8r8zD/bX5N2MHWiQdKoYnTO05gSIYTLAsrnUBnAUhTqFd+EzSvrp4WvfXw/eRx1VZv3yM0PKaduhUWumSWd4sSGSJdKRGUyLriDsNIIwVHWBHjUXh4q73skr8HxtbJ29dAqjdsuJju+UkhWcFOheKeBAoY9KUkuli1kPFp0Mu8FS2QSMqRmuxoRus73Tfdkr211XfbzR3feZ4l5SXEa3JeNCVuRBBoLucsqkalWMkgA6twYkkjG4E9VCEnGz4Xen7IupJXvzsiW2JtI/oYmnEkfRq/SGUENiIsGkOpUOGC6hlV3EbhwqmrT/wCzdjnfS3fw4+GrLIy+W7KrY8pLl7K6MjMLhOjlQFDEyxzFSIxqUZCsJE17wQAc1rnQgqsbaZrnpx88nYpVSTi+f0J1+UkykwtAn6T00cSqJSYj0kbSBi/RhhhUfI0ZyB9KqFQi/mT+W19M8nbS/wCSzpHpbPxIy15VTxxANEZrh573qjpHCrBOy6AY4mYgakUEqBjeccDbLDRlLJ2SUeXFd7XjqR6RpaZ5m1JyhlR55BGzZWwCQSEx6WuZGQhuqSpBYZ3ebUFQi0lf+2az0Vzu+7t+HqbGzuU0rSKs0EcaFp49YmLYkt8liQY1xGQCQ2c7t4qM6EUrxd9HpwfnqdjUbea9o0H5USTRnC9E63FiNSmTS8U86A46SNGIIDjOnBG8HfusWHjGXPKXLVJ8mziqb3obH9qpJTOiQOsarcqswWXAeDUuWJjEYBZWxh2IwMgEkCPw6iotvPLLLj539B0jbatzJLkhtdrmAORgDCAk9dmVRrZ1x8nls4B3kYO7OKqxFJU5W9/6Spy3lcidhbTuhbyXDJczsFJVW6Do3Icj5MRAy7gN+pSccATV1WnT31BNL63875EIOVm839DfTlKxa16kfRz7jNrk6MPq0iNT0Xlk56r6N40jJ4V9ArSzzXDK/jrp4X5kuk07/fvQ1V5Z46fMQZY01xsjPpk+U6MDU8aDiV6y6l3nfu3y+F6ueuumWV+DfrZnOm1yMI2tcrJMJToYXVnHpjkDoFl0agC8ecHO8aQeOCONS6ODS3eUnmuV+TOb8k3fmvUudYi8UAoBQCgFAeJoldSjqGVgQykZBBGCCDxBHZXU2ndHGr5M0bfYVsiNGsKhHwWG86tONOSd5AwMDsqbqzbu2cUIrKxmOzIdRfo11GQS57ekCCMP6dAC+iudJK1r8LeWv3O7qPjbKgIlUxIRMcyggEOdIXLd+4D2U6SWTvpoN1HyDZMKaNKD5NiyEksQzKVY5JJ3qSPXR1JO+epxRRjGw7fr/JLl2VmJySSj9InE7gH6wA3ZJrvSzyz9vIbqPs2xbduk1RKelKtJ9ZkxobwYYHWG/cO6iqzVrPQbqMp2ZEYTAUBhIKlDkght7A5OTnJznvrnSS3t6+Z2ytY+XuyoJcmWJX1JobIzlNQbSe8ZGaRqSjow4p6mAbAtejMXQroZg5G/JcYw+rOrUMAA5yABUumne9zm5G1geT1roWPoVCIzMoGV0s2dRUg5GcnOOOadNUve43I2tYzPsqAkkxqSeiJ8TC2qL+E7xUVUkuPP11O7qPv/AIXDnPRr5Tvw86QESH/MCc+mnSS5+0c3Ua9tyetYwQkKgExseJyYTqiO8+acY7sDuqTrVHq+frr9TihFaIyrsa3Du4iAaQMHxkBtXlEjOMntOMmudLOyV9Du6jNaWEUZYxoFLBQ2O3QulM+IXAz3Ad1RlOUtWdSS0NSLk9aqGCxBQwwcFh5wbdv3bwDuqbrTerI7kT2uw7YGMiJfk8aBvwCCWBxwJBJOTvyc8a50s889Tu5HkeIuT1ouvECYkVkcYyCjHJTB3acknTw3nvrrrVHbPQKEVoj1b7BtkGFhUAujniSXjwUYknJIwN57hXHWm9X3fUbkeRJVWSFAKAUAoBQCgFAKAUAoBQCgFAKAUAoBQCgFAKAUAoBQCgFAKAUAoBQCgFAKAUAoBQCgFAKAUAoBQCgFAKAUAoBQCgFAKAUAoBQCgFAf/9k=\" width =\"20%\" height=\"20%\"><hr><h1 style=\"text-align:center;\">Convention de stage</h1><hr><div style=\"font-size:14px;\"><br/><br/>La presente convention règle les rapports entre :<br/><br/> La socièté :<font color=\"red\">" + company + " </font> <br/><br/> Et l'Ecole Nationale des Sciences Appliquées de Tétouan <br/><br/>Concernant l'étudiant : <font color=\"red\">" + destinationName + " </font></div><div style=\"font-size:14px;\"><br/><br/>La durée de stage est de : <br/><br/><font color=\"red\">" + convention.StartDate + " </font><br/><br/>Jusqu'a : <font color=\"red\">" + convention.EndDate + "</font></div></body></head></html>";
                HtmlToPdf.ConvertHtml(msg, str);
                str.Position = 0;

                Attachment attachment = new Attachment(str, "Convention", MediaTypeNames.Application.Pdf);

                var fromAddress = new MailAddress("elyousfi.wail@gmail.com", "ENSA TETOUAN - ADMINISTRATION");
                var toAddress = new MailAddress(destinationEmail, destinationName);
                string fromPassword = ConfigurationManager.AppSettings["pass"];
                const string subject = "Convention de stage";
                string body = "Bonjour M/Mme " + destinationName + ", \n \nVeuillez trouver dans la pièce ci-jointe votre convention de stage \n\nCordialement.";


                smtp.Host = "smtp.gmail.com";
                smtp.Port = 587;
                smtp.EnableSsl = true;
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new NetworkCredential(fromAddress.Address, fromPassword);

                message.From = fromAddress;
                message.To.Add(toAddress);
                message.IsBodyHtml = true;
                message.Subject = subject;
                message.Body = body;
                message.Attachments.Add(attachment);
                smtp.Send(message);

            }
                
            

            return RedirectToAction("Index", "Conventions"); // index is the action name, conventions is the controller name.
        }

        [Authorize(Roles = "Admin")]
        public ActionResult Reject(int? id)
        {
            Convention convention = db.conventions.Include(u => u.Employee).Where(u => u.ConventionId == id).First();
            convention.State = "Rejetée";
            convention.AcceptanceDate = null;
            User currentUser = db.users.Where(u => u.Email == HttpContext.User.Identity.Name).First();
            convention.EmployeeId = currentUser.UserId;
            db.Entry(convention).State = EntityState.Modified;
            string destinationEmail = convention.Student.Email;
            string destinationName = convention.Student.Lastname + " " + convention.Student.Firstname;

            db.SaveChanges();

            var fromAddress = new MailAddress("elyousfi.wail@gmail.com", "ENSA TETOUAN - ADMINISTRATION");
            var toAddress = new MailAddress(destinationEmail, destinationName);
            string fromPassword = ConfigurationManager.AppSettings["pass"];
            const string subject = "Convention de stage";
            string body = "Bonjour M/Mme " + destinationName + ", \n \nVotre demande de convention de stage a été refusée, veuillez contacter l'administration pour plus d'informations. \n \nCordialement.";

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
            };
            using (var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = body
            })
            {
                smtp.Send(message);
            }

            return RedirectToAction("Index", "Conventions");
        }


        // GET: Conventions/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Convention convention = db.conventions.Find(id);
            if (convention == null)
            {
                return HttpNotFound();
            }
            ViewBag.EmployeeId = new SelectList(db.users, "UserId", "Firstname", convention.EmployeeId);
            ViewBag.StudentId = new SelectList(db.users, "UserId", "Firstname", convention.StudentId);
            return View(convention);
        }

        // POST: Conventions/Edit/5
        // Afin de déjouer les attaques par sur-validation, activez les propriétés spécifiques que vous voulez lier. Pour 
        // plus de détails, voir  https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ConventionId,CompanieName,StartDate,EndDate")] Convention convention)
        {
            if (ModelState.IsValid)
            {
                db.Entry(convention).State = EntityState.Modified;
                db.Entry(convention).Property(x => x.EmployeeId).IsModified = false; //  Pour ne pas changer l'ancienne valeur
                db.Entry(convention).Property(x => x.StudentId).IsModified = false;
                db.Entry(convention).Property(x => x.State).IsModified = false;
                db.Entry(convention).Property(x => x.AcceptanceDate).IsModified = false;
                db.Entry(convention).Property(x => x.RequestDate).IsModified = false;

                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.EmployeeId = new SelectList(db.users, "UserId", "Firstname", convention.EmployeeId);
            ViewBag.StudentId = new SelectList(db.users, "UserId", "Firstname", convention.StudentId);
            return View(convention);
        }

        // GET: Conventions/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Convention convention = db.conventions.Find(id);
            if (convention == null)
            {
                return HttpNotFound();
            }
            return View(convention);
        }

        // POST: Conventions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Convention convention = db.conventions.Find(id);
            db.conventions.Remove(convention);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
