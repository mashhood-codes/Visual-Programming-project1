
use argon2::Argon2;
use futures_util::lock::Mutex;
use hyper::StatusCode;
use mssql::{get_logs, get_orders, get_suppliers, run_query};
use routes::{ create_product, dashboard};
use serde::{Deserialize, Serialize};
use serde_json::to_string_pretty;
use tiberius::Config;
use tokio::io::AsyncWriteExt;
use tokio::net::TcpListener;
use axum::routing::{get,post};
use axum::{Json, Router, ServiceExt};
use tokio::fs::{File,OpenOptions};
mod mssql;
mod routes;
use routes::products::get_products;
use crate::routes::{update_product,search_products,delete_product};
mod auth;
use auth::login;
#[tokio::main]
async fn main(){
let q = mssql::run_query("select * from Products;".to_string()).await.unwrap();
let addr = "127.0.0.1:3500";
let listener = TcpListener::bind(addr).await.unwrap();
println!("Server is Listening for incoming! ...");
let routes= Router::new()
.route("/login", post(login))
.route("/logs", get(get_logs))
.route("/dashboard", get(dashboard))
.route("/products", get(get_products))
.route("/products/create", post(create_product))
.route("/products/update/:product_id", post(update_product))
.route("/products/delete", post(delete_product))
.route("/products/search", get(search_products))
.route("/supplier", get(get_suppliers))
.route("/orders", get(get_orders));
let server = axum::serve(listener, routes.into_make_service()).await.expect("Listening Failed! ...\n");
}
