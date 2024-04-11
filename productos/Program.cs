using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Npgsql;


namespace GestionProductos
{

[Table("Productos")]   //tabla en mi base de datos

    public class Producto
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]  //id: clave primaria
        [Column("Id")]
        public int Id { get; set; }

        [Column("CodigoEAN")]   //tal cual lo tenga en mi base de datos
        public string CodigoEAN { get; set; }

        [Column("Descripcion")]
        public string Descripcion { get; set; }

        [Column("TipoProducto")]
        public string TipoProducto { get; set; }

        [Column("PrecioUnitario")]
        public double PrecioUnitario { get; set; } // Cambiado de decimal a double

        [Column("PorcentajeIVA")]
        public double PorcentajeIVA { get; set; } // Cambiado de decimal a double

        public override string ToString()
        {
            return $"ID: {Id}, Código EAN: {CodigoEAN}, Descripción: {Descripcion}, Tipo de Producto: {TipoProducto}, Precio: {PrecioUnitario}, IVA: {PorcentajeIVA}";
        }
    }

//CONEXION A LA BASE DE DATOS POSTGRES.
    public class ProductoContext : DbContext
    {
        public DbSet<Producto> Productos { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("Host=localhost;Username=postgres;Password=9090;Database=productosBaseDato");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Producto>()
                .Property(p => p.Id)
                .ValueGeneratedOnAdd();
        }
    }
    
//CONEXION AL ARCHIVO CSV:
    
    class Program
{
    static string ubicacionArchivo; // Variable para almacenar la ubicación del archivo CSV
    static string separador = ";"; // Separador de campos en el archivo CSV

    static void Main(string[] args)
    {
        Console.WriteLine("----> Ingrese la ubicación del archivo CSV:");
        ubicacionArchivo = Console.ReadLine(); // pedir al usuario la ubicación del archivo

        using (var context = new ProductoContext())
        {
            context.Database.EnsureCreated(); // Crea la base de datos si no existe

            MostrarMenu();
        }
    }


    /*
    ubicacion  C:\\Users\\ASUS\\Documents\\ITES 2022\\2024\\supermercado\\productos.csv.csv
        

    */

        static void MostrarMenu()
        {
            while (true)
            {
                Console.WriteLine("\n        ***  MENÚ PRINCIPAL  ***");
                Console.WriteLine("1 - Importar productos desde un archivo CSV");
                Console.WriteLine("2 - Buscar productos por descripción");
                Console.WriteLine("3 - Visualizar detalles de un producto");
                Console.WriteLine("4 - Actualizar un producto");
                Console.WriteLine("5 - Mostrar todos los productos");  //PARA MOSTRAR TODO LO QUE HAY CARGADO
                Console.WriteLine("6 - Salir del programa");
                Console.Write("Seleccione una opción: ");

                if (int.TryParse(Console.ReadLine(), out int opcion))
                {
                    switch (opcion)
                    {
                        case 1:
                            ImportarProductosDesdeCSV();
                            break;
                        case 2:
                            BuscarProductosPorDescripcion();
                            break;
                        case 3:
                            VisualizarDetallesProducto();
                            break;
                        case 4:
                            ActualizarProducto();
                            break;
                        case 5:
                            MostrarTodosLosProductos();
                            break;
                        case 6:
                            Console.WriteLine(" *** SALISTE DEL PROGRAMA... *** ");
                            return;
                        default:
                            Console.WriteLine("---> Opción inválida. Intente de nuevo.");
                            break;
                    }
                }
                else
                {
                    Console.WriteLine(" ---> Opción inválida. Intente de nuevo.");
                }
            }
        }

//IMPORTAR PRODUCTOS CSV A LA BASE DE DATOS:
        static void ImportarProductosDesdeCSV()
        {
            try
            {
                List<Producto> productos = new List<Producto>();
                int productosNuevos = 0;
                
                using (StreamReader archivo = new StreamReader(ubicacionArchivo))
                {
                    archivo.ReadLine(); // Leer la primera línea y descartarla (encabezado)
                    string linea;
                    while ((linea = archivo.ReadLine()) != null)
        {
            string[] campos = linea.Split(separador);
            if (campos.Length >= 6)
            {
                if (int.TryParse(campos[0], out int id) && double.TryParse(campos[4], out double precio) && double.TryParse(campos[5], out double iva))
                {
                    using (var context = new ProductoContext())
                    {
                            // Verificar si el producto ya existe en la base de datos
                            
                            if (!context.Productos.Any(p => p.Id == id))
                            {
                                Producto producto = new Producto()
                                {
                                    Id = id,
                                    CodigoEAN = campos[1],
                                    Descripcion = campos[2],
                                    TipoProducto = campos[3],
                                    PrecioUnitario = precio,
                                    PorcentajeIVA = iva
                                };
                                productos.Add(producto);
                                productosNuevos++;
                            }
                    }
                }
                    else
                    {
                        Console.WriteLine(" ---> Error al leer datos en la línea: {0}", linea);
                    }
            }
                else
                {
                    Console.WriteLine(" ---> Error: la línea no contiene suficientes campos: {0}", linea);
                }
        }
    }

        using (var context = new ProductoContext())
        {
            context.AddRange(productos);
            context.SaveChanges();
        }

        if (productosNuevos == 0)
        {
            Console.WriteLine("*** PRODUCTOS IMPORTADOS EXITOSAMENTE: Todos los productos ya existen en la base de datos. ***");
        }
        else
        {
            Console.WriteLine($"*** PRODUCTO IMPORTADO EXITOSAMENTE: Se importó {productosNuevos} nuevo(s) producto(s). ***");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("---> Error al leer el archivo: {0}", ex.Message);

        // Imprimir la excepción interna si está disponible
        if (ex.InnerException != null)
        {
            Console.WriteLine("Inner Exception: {0}", ex.InnerException.Message);
        }
    }
}

//BUSCAR PRODUCTOS POR DESCRIPCION---
        static void BuscarProductosPorDescripcion()
        {
            Console.WriteLine("\n Ingrese la descripción del producto a buscar: ");
            string descripcion = Console.ReadLine();

            try
            {
                using (var context = new ProductoContext())
                {
                    var productosEncontrados = context.Productos.Where(p => p.Descripcion.ToLower().Contains(descripcion.ToLower())).ToList();

                    if (productosEncontrados.Any())
                    {
                        Console.WriteLine("\n PRODUCTOS ENCONTRADOS: ");
                        foreach (var producto in productosEncontrados)
                        {
                            Console.WriteLine(producto);
                        }
                    }
                    else
                    {
                        Console.WriteLine(" ---> No se encontraron productos con la descripción ingresada.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("---> Error al buscar productos: {0}", ex.Message);
            }
        }

//VISUALIZAR DETALLES DE UN PRODUCTO---
        static void VisualizarDetallesProducto()
        {
            Console.WriteLine("\n Ingrese el ID del producto:");
            if (int.TryParse(Console.ReadLine(), out int idProducto))
            {
                try
                {
                    using (var context = new ProductoContext())
                    {
                        var producto = context.Productos.Find(idProducto);

                        if (producto != null)
                        {
                            Console.WriteLine("\n DETALLES DEL PRODUCTO: ");
                            Console.WriteLine(producto);
                        }
                        else
                        {
                            Console.WriteLine(" ---> No se encontró un producto con el ID especificado.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("---> Error al buscar el producto: {0}", ex.Message);
                }
            }
        }

//ACTUALIZAR UN PRODUCTO---
        static void ActualizarProducto()
        {
            Console.WriteLine("\nIngrese el ID del producto a actualizar: ");
            if (int.TryParse(Console.ReadLine(), out int idProducto))
            {
                try
                {
                    using (var context = new ProductoContext())
                    {
                        var producto = context.Productos.Find(idProducto);

                        if (producto != null)
                        {
                            Console.WriteLine("\nDETALLES DEL PRODUCTO A ACTUALIZAR:");
                            Console.WriteLine(producto);

                            // Solicitar la actualización de campos
                            Console.WriteLine("\nIngrese el nuevo precio:");
                            double nuevoPrecio = double.Parse(Console.ReadLine());
                            Console.WriteLine("Ingrese el nuevo tipo de producto:");
                            string nuevoTipoProducto = Console.ReadLine();
                            Console.WriteLine("Ingrese la nueva descripción:");
                            string nuevaDescripcion = Console.ReadLine();
                            Console.WriteLine("Ingrese el nuevo porcentaje de IVA:");
                            double nuevoIVA = double.Parse(Console.ReadLine());

                            // Actualizar los campos del producto
                            producto.PrecioUnitario = nuevoPrecio;
                            producto.TipoProducto = nuevoTipoProducto;
                            producto.Descripcion = nuevaDescripcion;
                            producto.PorcentajeIVA = nuevoIVA;

                            context.SaveChanges();
                            Console.WriteLine("\n¡Producto actualizado con éxito!");
                        }
                        else
                        {
                            Console.WriteLine(" ---> No se encontró un producto con el ID especificado.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("---> Error al actualizar el producto: {0}", ex.Message);
                }
            }
        }

//MUESTRA TODOS LOS PRODUCTOS CARGADOS ---
        static void MostrarTodosLosProductos()
        {
            using (var context = new ProductoContext())
            {
                var productos = context.Productos.ToList();
                Console.WriteLine("*** LISTA DE TODOS LOS PRODUCTOS ***");
                foreach (var producto in productos)
                {
                    Console.WriteLine(producto);
                }
            }
        }
    }
}

