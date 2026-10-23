-- Where a JSON-answering shop keeps its products, learned from an example album when the shop is added. NULL = HTML shop.
ALTER TABLE shops
    ADD COLUMN json_items_path VARCHAR(500),
    ADD COLUMN json_url_field VARCHAR(200);
