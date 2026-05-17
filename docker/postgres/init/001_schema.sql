CREATE TABLE IF NOT EXISTS price_ticks (
    stock_exchange_id INT NOT NULL,
    ticker_id INT NOT NULL,
    timestamp TIMESTAMPTZ NOT NULL,
    price NUMERIC(18, 9) NOT NULL,
    volume BIGINT NOT NULL,
    PRIMARY KEY (stock_exchange_id, ticker_id, timestamp)
);
