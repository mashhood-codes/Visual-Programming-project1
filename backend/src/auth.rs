use axum::{
    extract::FromRequestParts,
    http::{header::SET_COOKIE, HeaderMap, StatusCode},
    Json,
};
use chrono::{Duration, Utc};
use jsonwebtoken::{decode, encode, DecodingKey, EncodingKey, Header, Validation};
use serde::{Deserialize, Serialize};

const JWT_SECRET: &[u8] = b"Inventory"; 
const TOKEN_EXPIRY_HOURS: i64 = 1;

#[derive(Debug, Serialize, Deserialize)]
pub struct Claims {
    pub sub: String,
    pub exp: i64,
    pub iat: i64,
}

#[derive(Debug, Deserialize)]
pub struct Credentials {
    username: String,
    password: String,
}

#[derive(Debug)]
pub struct AuthenticatedUser {
    pub user_id: String,
}

#[derive(Debug, Serialize)]
pub struct ErrorResponse {
    error: String,
}

impl Claims {
    fn new(username: &str) -> Self {
        let now = Utc::now();
        Self {
            sub: username.to_string(),
            iat: now.timestamp(),
            exp: (now + Duration::hours(TOKEN_EXPIRY_HOURS)).timestamp(),
        }
    }
}
pub fn validate_jwt(token: &str) -> Result<Claims, jsonwebtoken::errors::Error> {
    let validation = Validation::default();
    let token_data = decode::<Claims>(
        token,
        &DecodingKey::from_secret(JWT_SECRET),
        &validation,
    )?;
    Ok(token_data.claims)
}

pub fn is_valid_jwt(token: &str) -> bool {
    validate_jwt(token).is_ok()
}

pub fn get_user_from_token(token: &str) -> Option<String> {
    validate_jwt(token).map(|claims| claims.sub).ok()
}

fn generate_jwt(username: &str) -> Result<String, jsonwebtoken::errors::Error> {
    let claims = Claims::new(username);
    encode(
        &Header::default(),
        &claims,
        &EncodingKey::from_secret(JWT_SECRET),
    )
}

fn create_cookie(token: &str) -> String {
    format!(
        "token={}; HttpOnly; Secure; Path=/; Max-Age={}; SameSite=Strict",
        token,
        TOKEN_EXPIRY_HOURS * 3600
    )
}

pub async fn login(Json(cred): Json<Credentials>) -> (StatusCode, HeaderMap, Json<serde_json::Value>) {
 
    if cred.username == "admin" && cred.password == "admin" {
        match generate_jwt(&cred.username) {
            Ok(token) => {
                let mut headers = HeaderMap::new();
                let cookie = create_cookie(&token);
                if let Ok(cookie_value) = cookie.parse() {
                    headers.insert(SET_COOKIE, cookie_value);
                    (
                        StatusCode::OK,
                        headers,
                        Json(serde_json::json!({
                            "message": "Login successful",
                            "token": token
                        })),
                    )
                } else {
                    (
                        StatusCode::INTERNAL_SERVER_ERROR,
                        HeaderMap::new(),
                        Json(serde_json::json!({"error": "Failed to create session"})),
                    )
                }
            }
            Err(_) => (
                StatusCode::INTERNAL_SERVER_ERROR,
                HeaderMap::new(),
                Json(serde_json::json!({"error": "Failed to generate token"})),
            ),
        }
    } else {
        (
            StatusCode::UNAUTHORIZED,
            HeaderMap::new(),
            Json(serde_json::json!({"error": "Invalid credentials"})),
        )
    }
}

pub fn extract_token_from_cookie(cookie_header: Option<&str>) -> Option<&str> {
    cookie_header?
        .split(';')
        .find_map(|cookie| {
            let mut parts = cookie.trim().split('=');
            match (parts.next(), parts.next()) {
                (Some(key), Some(value)) if key == "token" => Some(value),
                _ => None,
            }
        })
}

impl<S> FromRequestParts<S> for AuthenticatedUser
where
    S: Send + Sync,
{
    type Rejection = (StatusCode, Json<ErrorResponse>);

    fn from_request_parts<'life0, 'life1, 'async_trait>(
        parts: &'life0 mut axum::http::request::Parts,
        _state: &'life1 S,
    ) -> std::pin::Pin<Box<dyn std::future::Future<Output = Result<Self, Self::Rejection>> + Send + 'async_trait>>
    where
        'life0: 'async_trait,
        'life1: 'async_trait,
        Self: 'async_trait,
    {
        Box::pin(async move {
            let unauthorized = || {
                (
                    StatusCode::UNAUTHORIZED,
                    Json(ErrorResponse {
                        error: "Invalid or missing token".to_string(),
                    }),
                )
            };

            let cookie_header = parts
                .headers
                .get("cookie")
                .and_then(|value| value.to_str().ok());

            let token = match extract_token_from_cookie(cookie_header) {
                Some(token) => token,
                None => return Err(unauthorized()),
            };

            let token_data = match decode::<Claims>(
                token,
                &DecodingKey::from_secret(JWT_SECRET),
                &Validation::default(),
            ) {
                Ok(data) => data,
                Err(_) => return Err(unauthorized()),
            };

            Ok(AuthenticatedUser {
                user_id: token_data.claims.sub,
            })
        })
    }
}

pub async fn logout() -> (StatusCode, HeaderMap) {
    let mut headers = HeaderMap::new();
    headers.insert(
        SET_COOKIE,
        "token=; HttpOnly; Secure; Path=/; Max-Age=0; SameSite=Strict"
            .parse()
            .unwrap(),
    );
    (StatusCode::OK, headers)
}

pub async fn protected_route(
    auth_user: AuthenticatedUser,
) -> Result<Json<serde_json::Value>, (StatusCode, Json<ErrorResponse>)> {
    Ok(Json(serde_json::json!({
        "message": "This is a protected route",
        "user_id": auth_user.user_id
    })))
}

pub fn extract_token_from_header(headers: &HeaderMap) -> Option<String> {
    headers
        .get("Authorization")
        .and_then(|value| value.to_str().ok())
        .and_then(|auth_str| {
            if auth_str.starts_with("Bearer ") {
                Some(auth_str[7..].to_string())
            } else {
                None
            }
        })
}