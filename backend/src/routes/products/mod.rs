use crate::mssql::{run_query, QueryResult};
use axum::{http::Request, Json};
use hyper::StatusCode;


pub async fn get_products(_cookie:String)->(StatusCode,String){

// let flag = validate_cookie(cookie,3);
// if flag == false {
//     return (403,"".to_string();
// }


let res = run_query("Select * from Products;".to_string()).await;
let data = match res {
    Ok(d)=>{
        d
    }
    Err(e)=>{
        return (StatusCode::FORBIDDEN,"".to_string());
    }
};
return (StatusCode::OK,serde_json::to_string_pretty(&data).unwrap_or("".to_string()));
}












