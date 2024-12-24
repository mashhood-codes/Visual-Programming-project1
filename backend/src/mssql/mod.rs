use std::vec;

use axum::extract::Query;
use chrono::DateTime;
use futures_util::StreamExt;
use hyper::StatusCode;
use tiberius::{Config, Client,ColumnType,Row};
use tokio::{io::AsyncWriteExt, net::TcpStream};
use tokio_util::compat::{self, TokioAsyncReadCompatExt};
use serde::{Deserialize,Serialize};
use tokio::fs::{File,OpenOptions};
use serde_json::to_string_pretty;
use tokio_util::compat::Compat;
use axum::Json;
use axum::response::IntoResponse;
use tiberius::time::chrono::NaiveDateTime;
use tiberius::time::DateTime as TiberiusDateTime;
use tiberius::numeric::Decimal;

#[derive(Debug, Serialize, Deserialize)]
pub struct QueryResult {
    pub rows: Vec<Vec<Option<String>>>, 
    pub columns: Vec<String>,        
}

impl QueryResult {
   pub fn new()->Self{
QueryResult{
    rows:vec![vec![Some("".to_string())]],
columns:vec!["".to_string()],
}
   }
    pub fn display(&self) {
        println!("Columns: {:?}", self.columns);

        for (i, row) in self.rows.iter().enumerate() {
            println!("Row {}: {:?}", i + 1, row);
        }
    }

   
    pub fn get_row(&self, index: usize) -> Option<&Vec<Option<String>>> {
        self.rows.get(index)
    }

    pub fn get_value(&self, row_idx: usize, col_idx: usize) -> Option<&Option<String>> {
        self.rows.get(row_idx)?.get(col_idx)
    }
}

pub async fn run_query(qstring: String) -> Result<QueryResult, Box<dyn std::error::Error>> {
   
    let mut db_config = Config::new();
    db_config.host("localhost");
    db_config.port(1433);
    db_config.authentication(tiberius::AuthMethod::Integrated);
    db_config.trust_cert();
    db_config.instance_name("(localdb)\\MSSQLLocalDB");
    db_config.database("Inventory");

 
    let stream = TcpStream::connect("127.0.0.1:1433").await?;
    let compat_stream = stream.compat();
    let mut dbconnection = Client::connect(db_config, compat_stream).await?;

    let mut results = dbconnection.query(qstring, &[]).await?;

    let mut rows = Vec::new();
    let mut column_names = Vec::new();

    while let Some(item) = results.next().await {
        match item {
            Ok(query_item) => {
                if let Some(row) = query_item.as_row() {
                 
                    if column_names.is_empty() {
                        column_names = row
                            .columns()
                            .iter()
                            .map(|col| col.name().to_string())
                            .collect();
                    }

                    let row_values = row
                        .columns()
                        .iter()
                        .enumerate()
                        .map(|(i, col)| {
                            match col.column_type() {
                                ColumnType::Int4 => row.get::<i32, _>(i).map(|v| v.to_string()),
                                ColumnType::Int8 => row.get::<i64, _>(i).map(|v| v.to_string()),
                                ColumnType::Float4 | ColumnType::Float8 => {
                                    row.get::<f64, _>(i).map(|v| v.to_string())
                                }
                                ColumnType::NVarchar | ColumnType::NVarchar | ColumnType::Text => {
                                    row.get::<&str, _>(i).map(|v| v.to_string())
                                }
                                ColumnType::Bit => row.get::<bool, _>(i).map(|v| v.to_string()),
                           
                                _ => None, 
                            }
                        })
                        .collect::<Vec<Option<String>>>();
                 



                    rows.push(row_values);
                }
            }
            Err(e) => {
                eprintln!("Error processing row: {}", e);
            }
        }
    }



let data =  QueryResult {
        rows,
        columns: column_names,
    };

    let mut file = OpenOptions::new()
    .write(true)
    .create(true)
    .truncate(false)
    .open("./src/Requests_received.txt")
    .await
    .expect("Failed to open file");
    
    
    let _file_writer = tokio::fs::File::write_all(&mut file, to_string_pretty(&data).unwrap().as_bytes()).await.unwrap();
    Ok(data)
    
}
#[derive(Serialize,Deserialize,Debug)]
pub struct Supplier {
    purchase_order_id: i32,
    order_date: String,
    status: String,
    total_amount: f64,
    supplier_name: String,
    contact_name: Option<String>,
    phone: Option<String>,
    email: Option<String>,
    address: Option<String>,
}
pub async fn create_db_client() -> Result<Client<tokio_util::compat::Compat<TcpStream>>, tiberius::error::Error> {
    println!("Starting database connection...");

    let mut config = Config::new();
    config.host("localhost");
    config.port(1433);
    config.authentication(tiberius::AuthMethod::Integrated);
    config.trust_cert();
    config.instance_name("(localdb)\\MSSQLLocalDB");
    config.database("Inventory");

    let tcp = TcpStream::connect(config.get_addr()).await?.compat();
    let client = Client::connect(config, tcp).await?;

    println!("Database client created successfully.");
    Ok(client)
}

pub async fn fetch_supplier_rows(client: &mut Client<tokio_util::compat::Compat<TcpStream>>) -> Result<Vec<Row>, tiberius::error::Error> {
    println!("Executing query to fetch suppliers...");

    let query = r#"
        SELECT 
            po.PurchaseOrderID AS purchase_order_id,
            po.OrderDate AS order_date,
            po.Status AS status,
            po.TotalAmount AS total_amount,
            s.SupplierName AS supplier_name,
            s.ContactName AS contact_name,
            s.Phone AS phone,
            s.Email AS email,
            s.Address AS address
        FROM 
            PurchaseOrders po
        INNER JOIN 
            Suppliers s ON po.SupplierID = s.SupplierID
    "#;

    let stream = client.query(query, &[]).await?;
    let rows = stream.into_first_result().await?;

    println!("Query executed successfully. Rows fetched: {}", rows.len());
    Ok(rows)
}
pub fn parse_supplier_rows(rows: Vec<Row>) -> Vec<Supplier> {
    println!("Parsing rows into Supplier structs...");
    let suppliers:Vec<Supplier> = rows.into_iter().map(|row| {
        println!("Row Data: {:?}", row);
        
        let order_date = match row.get::<NaiveDateTime, _>("order_date") {
            Some(naive_date) => {
                naive_date.format("%Y-%m-%d %H:%M:%S").to_string()
            }
            None => "N/A".to_string(),
        };

        let total_amount = row.get::<Decimal, _>("total_amount")
        .map(|d| d.mantissa() as f64 / 10_f64.powi(d.scale() as i32)) 
            .unwrap_or(0.0);

        Supplier {
            purchase_order_id: row.get("purchase_order_id").unwrap_or(0),
            order_date,
            status: row.get::<&str, _>("status").unwrap_or("").to_string(),
            total_amount,
            supplier_name: row.get::<&str, _>("supplier_name").unwrap_or("").to_string(),
            contact_name: row.get::<&str, _>("contact_name").map(|s| s.to_string()),
            phone: row.get::<&str, _>("phone").map(|s| s.to_string()),
            email: row.get::<&str, _>("email").map(|s| s.to_string()),
            address: row.get::<&str, _>("address").map(|s| s.to_string()),
        }
    }).collect();
    
    println!("Parsed {} supplier(s).", suppliers.len());
    suppliers
}

#[derive(Serialize, Deserialize, Debug)]
pub struct SupplierResponse {
    status: String,
    data: Vec<Supplier>,
}
pub async fn get_suppliers() -> Json<SupplierResponse>{
    println!("Starting `get_suppliers` handler...");
    match create_db_client().await {
        Ok(mut client) => {
            match fetch_supplier_rows(&mut client).await {
                Ok(rows) => {
                    let suppliers = parse_supplier_rows(rows);
                    println!("Successfully fetched and parsed suppliers.");
                    Json(SupplierResponse{
                        status:StatusCode::OK.to_string(),
                        data:suppliers,
                    })
                }
                Err(err) => {
                    eprintln!("Failed to fetch suppliers: {}", err);
                    Json(SupplierResponse { status: StatusCode::INTERNAL_SERVER_ERROR.to_string(), data: vec![], })
                }
            }
        }
        Err(err) => {
            eprintln!("Failed to connect to the database: {}", err);
            Json(SupplierResponse { status: StatusCode::GATEWAY_TIMEOUT.to_string(), data: vec![],})
        }
    }
}













#[derive(Debug, Serialize, Deserialize)]
pub struct AuditLog {
    logid: i32,
    username: String,
    role: String,
    action: String,
    tableaffected: String,
    action_time: String,
    description: String,
}

pub async fn fetch_audit_logs(client: &mut Client<Compat<TcpStream>>) -> Result<Vec<Row>, tiberius::error::Error> {
    println!("Executing query to fetch audit logs...");

    let query = r#"
        SELECT 
            A.LogID,
            U.Username,
            U.Role,
            A.Action,
            A.TableAffected,
            A.ActionTime,
            A.Description
        FROM 
            AuditLogs A
        JOIN 
            Users U ON A.UserID = U.UserID
        ORDER BY 
            A.ActionTime DESC;
    "#;

    let stream = client.query(query, &[]).await?;
    let rows = stream.into_first_result().await?;

    println!("Query executed successfully. Rows fetched: {}", rows.len());
    Ok(rows)
}

pub fn parse_audit_rows(rows: Vec<Row>) -> Vec<AuditLog> {
    println!("Parsing rows into AuditLog structs...");
    let logs: Vec<AuditLog> = rows.into_iter().map(|row| {
        println!("Row Data: {:?}", row);
        
        let action_time = row
            .get::<NaiveDateTime, _>("ActionTime")
            .map(|naive_date| naive_date.format("%Y-%m-%d %H:%M:%S").to_string())
            .unwrap_or_else(|| "N/A".to_string());

        AuditLog {
            logid: row.get::<i32, _>("LogID").unwrap_or(0),
            username: row.get::<&str, _>("Username").unwrap_or("N/A").to_string(),
            role: row.get::<&str, _>("Role").unwrap_or("N/A").to_string(),
            action: row.get::<&str, _>("Action").unwrap_or("N/A").to_string(),
            tableaffected: row.get::<&str, _>("TableAffected").unwrap_or("N/A").to_string(),
            action_time,
            description: row.get::<&str, _>("Description").unwrap_or("N/A").to_string(),
        }
    }).collect();

    println!("Parsed {} audit log(s).", logs.len());
    logs
}

#[derive(Serialize, Deserialize, Debug)]
pub struct AuditResponse {
    status: String,
    data: Vec<AuditLog>,
}

pub async fn get_logs() -> impl IntoResponse {
    println!("Starting get_logs...");

    let result = async {
        let mut client = create_db_client().await?;
        let rows = fetch_audit_logs(&mut client).await?;
        Ok::<_, tiberius::error::Error>(rows)
    }
    .await;

    match result {
        Ok(rows) => {
            let logs = parse_audit_rows(rows);
            Json(AuditResponse {
                status: "success".to_string(),
                data: logs,
            })
        }
        Err(e) => {
            eprintln!("Error during logs retrieval: {}", e);
            Json(AuditResponse {
                status: "error".to_string(),
                data: Vec::new(),
            })
        }
    }
}















#[derive(Debug, Serialize, Deserialize)]
pub struct OrderManagementData {
    pub purchase_order_id: Option<i32>,
    pub supplier_name: Option<String>,
    pub purchase_order_date: Option<String>,   
    pub purchase_order_status: Option<String>,
    pub purchase_total_amount: f64,
    pub sales_order_id: Option<i32>,
    pub customer_name: Option<String>,
    pub sales_order_date: Option<String>,      
    pub sales_order_status: Option<String>,
    pub sales_total_amount: f64,
    pub movement_id: Option<i32>,
    pub movement_type: Option<String>,
    pub movement_quantity: Option<i32>,
    pub movement_date: Option<String>,      
    pub movement_description: Option<String>
}
pub async fn fetch_order_management_data(
    client: &mut Client<Compat<TcpStream>>
) -> Result<Vec<Row>, tiberius::error::Error> {
    println!("Executing query to fetch order management data...");

    let query = r#"
   

SELECT 
    po.PurchaseOrderID as purchase_order_id,
    s.SupplierName as supplier_name,
    po.OrderDate as purchase_order_date,
    po.Status as purchase_order_status,
    po.TotalAmount as purchase_total_amount,
    NULL as sales_order_id,
    NULL as customer_name,
    NULL as sales_order_date,
    NULL as sales_order_status,
    NULL as sales_total_amount,
    NULL as movement_id,
    NULL as movement_type,
    NULL as movement_quantity,
    NULL as movement_date,
    NULL as movement_description,
    po.OrderDate as order_date -- Add this to use in ORDER BY
FROM 
    PurchaseOrders po
JOIN 
    Suppliers s ON po.SupplierID = s.SupplierID

UNION ALL

SELECT 
    NULL as purchase_order_id,
    NULL as supplier_name,
    NULL as purchase_order_date,
    NULL as purchase_order_status,
    NULL as purchase_total_amount,
    so.SalesOrderID as sales_order_id,
    so.CustomerName as customer_name,
    so.OrderDate as sales_order_date,
    so.Status as sales_order_status,
    so.TotalAmount as sales_total_amount,
    NULL as movement_id,
    NULL as movement_type,
    NULL as movement_quantity,
    NULL as movement_date,
    NULL as movement_description,
    so.OrderDate as order_date -- Add this to use in ORDER BY
FROM 
    SalesOrders so

UNION ALL

SELECT 
    NULL as purchase_order_id,
    NULL as supplier_name,
    NULL as purchase_order_date,
    NULL as purchase_order_status,
    NULL as purchase_total_amount,
    NULL as sales_order_id,
    NULL as customer_name,
    NULL as sales_order_date,
    NULL as sales_order_status,
    NULL as sales_total_amount,
    sm.MovementID as movement_id,
    sm.MovementType as movement_type,
    sm.Quantity as movement_quantity,
    sm.MovementDate as movement_date,
    sm.Description as movement_description,
    sm.MovementDate as order_date -- Add this to use in ORDER BY
FROM 
    StockMovements sm

ORDER BY 
    order_date DESC;  -- Use the common alias for the ORDER BY
 
    "#;

    let stream = client.query(query, &[]).await?;
    let rows = stream.into_first_result().await?;
    println!("Query executed successfully. Rows fetched: {}", rows.len());
    Ok(rows)
}

pub fn parse_order_management_rows(rows: Vec<Row>) -> Vec<OrderManagementData> {
  







    println!("Parsing rows into OrderManagementData structs...");

    rows.into_iter().map(|row| {
        println!("Row Data: {:?}", row);

        let format_date = |naive_date: Option<NaiveDateTime>| {
            naive_date.map(|d| d.format("%Y-%m-%d %H:%M:%S").to_string())
        };
        let format_decimal = |dec: Option<Decimal>| {
            dec.map(|d|d.mantissa() as f64 / 10_f64.powi(d.scale() as i32)) 
            .unwrap_or(0.0) 
        };

        OrderManagementData {
            purchase_order_id: row.get("purchase_order_id"),
            supplier_name: row.get::<&str, _>("supplier_name").map(String::from),
            purchase_order_date: format_date(row.get("purchase_order_date")),
            purchase_order_status: row.get::<&str, _>("purchase_order_status").map(String::from),
            purchase_total_amount: format_decimal(row.get("purchase_total_amount")),
            sales_order_id: row.get("sales_order_id"),
            customer_name: row.get::<&str, _>("customer_name").map(String::from),
            sales_order_date: format_date(row.get("sales_order_date")),
            sales_order_status: row.get::<&str, _>("sales_order_status").map(String::from),
            sales_total_amount: format_decimal(row.get("sales_total_amount")),
            movement_id: row.get("movement_id"),
            movement_type: row.get::<&str, _>("movement_type").map(String::from),
            movement_quantity: row.get("movement_quantity"),
            movement_date: format_date(row.get("movement_date")),
            movement_description: row.get::<&str, _>("movement_description").map(String::from),
        }
    }).collect()
}

#[derive(Serialize, Deserialize, Debug)]
pub struct OrderResponse {
    status: String,
    data: Vec<OrderManagementData>
}

pub async fn get_orders() -> Json<OrderResponse> {
    println!("Starting get_orders handler...");
    
    match create_db_client().await {
        Ok(mut client) => {
            match fetch_order_management_data(&mut client).await {
                Ok(rows) => {
                    let orders = parse_order_management_rows(rows);
                    println!("Successfully fetched and parsed orders.");
                    Json(OrderResponse {
                        status: StatusCode::OK.to_string(),
                        data: orders,
                    })
                }
                Err(err) => {
                    eprintln!("Failed to fetch orders: {}", err);
                    Json(OrderResponse {
                        status: StatusCode::INTERNAL_SERVER_ERROR.to_string(),
                        data: vec![],
                    })
                }
            }
        }
        Err(err) => {
            eprintln!("Failed to connect to database: {}", err);
            Json(OrderResponse {
                status: StatusCode::GATEWAY_TIMEOUT.to_string(),
                data: vec![],
            })
        }
    }
}






