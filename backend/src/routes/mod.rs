pub mod products;
use axum::Json;
use futures_util::StreamExt;
use hyper::StatusCode;
pub use products::*;
use serde::{Deserialize,Serialize};
use tiberius::{Client, Config, Row};
use tokio::net::TcpStream;
use tokio_util::compat::{TokioAsyncWriteCompatExt,TokioAsyncReadCompatExt};
use axum::extract::Query;
use crate::mssql::{run_query,QueryResult};
use axum::{
    extract::Path,
};
use std::collections::HashMap;
use axum::response::IntoResponse;
//#[derive(Deserialize, Debug,Serialize)]
// pub struct Product {
//     #[serde(rename = "ProductID")]
//     product_id: String,
//     #[serde(rename = "Name")]
//     name: String,
//     #[serde(rename = "SKU")]
//     sku: String,
//     #[serde(rename = "CategoryName")]
//     category_name: String,
//     #[serde(rename = "Quantity")]
//     quantity: String,
//     #[serde(rename = "UnitPrice")]
//     unit_price: String,
//     #[serde(rename = "Barcode")]
//     barcode: String,
// }




#[derive(Deserialize, Debug, Serialize)]
pub struct Product {
    #[serde(rename = "product_id")]
    product_id: String,
    #[serde(rename = "name")]
    name: String,
    #[serde(rename = "sku")]
    sku: String,
    #[serde(rename = "category_name")]
    category_name: String,
    #[serde(rename = "quantity")]
    quantity: String,
    #[serde(rename = "unit_price")]
    unit_price: String,
    #[serde(rename = "barcode")]
    barcode: String,

}
#[derive(Deserialize, Debug, Serialize)]
pub struct Productcreate {
    #[serde(rename = "name")]
    name: String,
    #[serde(rename = "sku")]
    sku: String,
    #[serde(rename = "category_name")]
    category_name: String,
    #[serde(rename = "quantity")]
    quantity: String,
    #[serde(rename = "unit_price")]
    unit_price: String,
    #[serde(rename = "barcode")]
    barcode: String,

}


pub async fn create_product(Json(product): Json<Productcreate>) -> StatusCode {
    let query = format!(
        "INSERT INTO Products (Name, SKU, CategoryName, Quantity, UnitPrice, Barcode) \
         VALUES ('{}', '{}', '{}', {}, {}, '{}')",
        product.name, 
        product.sku, 
        product.category_name,
        product.quantity,
        product.unit_price,
        product.barcode
    );
    match run_query(query.to_string()).await {
        Ok(_) => StatusCode::CREATED,
        Err(e) => {
            eprintln!("Failed to insert product: {:?}", e);
            StatusCode::INTERNAL_SERVER_ERROR
        }
    }
    
}


pub async fn update_product(Json(product): Json<Product>) -> StatusCode {
    let update_field = if !product.name.is_empty() {
        Some(("Name", product.name))
    } else if !product.sku.is_empty() {
        Some(("SKU", product.sku))
    } else if !product.category_name.is_empty() {
        Some(("CategoryName", product.category_name))
    } else if !product.quantity.is_empty() {
        Some(("Quantity", product.quantity))
    } else if !product.unit_price.is_empty() {
        Some(("UnitPrice", product.unit_price))
    } else if !product.barcode.is_empty() {
        Some(("Barcode", product.barcode))
    } else {
        None
    };

    match update_field {
        Some((field, value)) => {
            let query = format!(
                "UPDATE Products SET {} = '{}' WHERE ProductID = '{}'",
                field.trim(),
                value.trim(),
                product.product_id
            );
            
            match run_query(query.to_string()).await {
                Ok(_) => StatusCode::OK,
                Err(_) => StatusCode::INTERNAL_SERVER_ERROR,
            }
        },
        None => StatusCode::BAD_REQUEST,
    }
}




#[derive(Deserialize,Serialize,Debug)]
pub struct ProductId {
    product_id: i64,
}
pub async fn delete_product(Json(payload): Json<ProductId>) -> impl IntoResponse {
    let query = format!(
        "EXEC DeleteProductWithMovements @ProductID = {}",
        payload.product_id
    );
println!("ID: {}",payload.product_id);
    match run_query(query).await {
        Ok(_) => StatusCode::OK,
        Err(_) => StatusCode::INTERNAL_SERVER_ERROR,
    }
}
pub async fn search_products(Query(params): Query<HashMap<String, String>>) -> Result<Json<QueryResult>, StatusCode> {
    let term = match params.get("term") {
        Some(t) => t,
        None => return Err(StatusCode::BAD_REQUEST),
    };

    let query = format!(
        "SELECT ProductID, Name, SKU, CategoryName, Quantity, UnitPrice, Barcode, CreatedAt, UpdatedAt \
         FROM Products WHERE \
         ProductID LIKE '%{}%' OR \
         Name LIKE '%{}%' OR \
         SKU LIKE '%{}%' OR \
         CategoryName LIKE '%{}%' OR \
         Barcode LIKE '%{}%'",
        term, term, term, term, term
    );

    match run_query(query.to_string()).await {
        Ok(result) => Ok(Json(result)),
        Err(_) => Err(StatusCode::INTERNAL_SERVER_ERROR),
    }
}



#[derive(Serialize)]
pub struct DashboardDetails {
    total_productsnum: i64,
    totalsalesnum: i64,
    lowstockalert: i64,
}
pub async fn execute_count_query(query: &str) -> Result<i64, StatusCode> {
    let mut count_value:i64 = 0;
    // Configure database connection
    let mut db_config = Config::new();
    db_config.host("localhost");
    db_config.port(1433);
    db_config.authentication(tiberius::AuthMethod::Integrated);
    db_config.trust_cert();
    db_config.instance_name("(localdb)\\MSSQLLocalDB");
    db_config.database("Inventory");

    let tcp = match TcpStream::connect(db_config.get_addr()).await{
Ok(v)=>v,
Err(_)=>{return  Err(StatusCode::INTERNAL_SERVER_ERROR);
}
    };
    let tcp = tcp.compat_write();
    let mut client = match Client::connect(db_config, tcp).await{
Ok(val)=>val,
Err(_)=>{
    return Err(StatusCode::INTERNAL_SERVER_ERROR);
}
    };

    if let Some(row) = client.query(query, &[]).await.unwrap().into_row().await.unwrap() {
     
        let count: i32 = row.get(0).expect("Expected a count value");
count_value = count as i64;
      //  println!("Count of Products: {}", count);
    }

    Ok(count_value)
}


pub async fn dashboard(_cookie: String) -> (StatusCode, Json<DashboardDetails>) {
   
    let total_productsnum_query = "SELECT COUNT(*) FROM Products";
    let totalsalesnum_query = "SELECT COUNT(*) FROM SalesOrders WHERE Status = 'Shipped'";
    let lowstockalert_query = "SELECT COUNT(*) FROM Products WHERE Quantity < 10";

    let total_productsnum = match execute_count_query(total_productsnum_query).await {
        Ok(count) => count,
        Err(_status) => {
            eprintln!("Failed to fetch total_productsnum");
            0
        }
    };

    let totalsalesnum = match execute_count_query(totalsalesnum_query).await {
        Ok(count) => count,
        Err(_status) => {
            eprintln!("Failed to fetch totalsalesnum");
            0
        }
    };

    let lowstockalert = match execute_count_query(lowstockalert_query).await {
        Ok(count) => count,
        Err(_status) => {
            eprintln!("Failed to fetch lowstockalert");
            0
        }
    };

    let dashboard_data = DashboardDetails {
        total_productsnum,
        totalsalesnum,
        lowstockalert,
    };

    (StatusCode::OK, Json(dashboard_data))
}



















