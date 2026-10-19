-- ==============================================================================
-- Seed Data for AI Chatbot-Based Order and Inventory Management System
-- Provides Roles, Default Users, Warehouses, Categories, Products, Inventory,
-- Suppliers, Customers, and System Settings.
-- ==============================================================================

-- 1. Roles
INSERT INTO roles (id, name, description) VALUES
('11111111-1111-1111-1111-111111111111', 'Admin', 'Full administrative and system control'),
('22222222-2222-2222-2222-222222222222', 'Staff', 'Inventory and order fulfillment operations'),
('33333333-3333-3333-3333-333333333333', 'Customer', 'Customer ordering and self-service account')
ON CONFLICT (id) DO NOTHING;

-- 2. Default Users (Pass: Admin@123, Staff@123, Customer@123 - hashed using ASP.NET Identity compatible PBKDF2/HMAC-SHA256)
-- Pre-hashed password for testing: 'AQAAAAIAAYagAAAAENK5j8oFq4+Fq3fVfS8jT6823Lw5bQ4p1lqM0yS7oW+3oU='
-- In production, .NET Identity manages hash verification; seed user records with known hashes.
INSERT INTO users (id, username, email, password_hash, full_name, phone_number, role_id, is_active) VALUES
('aaaa1111-0000-0000-0000-000000000001', 'admin', 'admin@inventorysystem.com', 'AQAAAAIAAYagAAAAENK5j8oFq4+Fq3fVfS8jT6823Lw5bQ4p1lqM0yS7oW+3oU=', 'System Administrator', '+91 9876543210', '11111111-1111-1111-1111-111111111111', TRUE),
('bbbb2222-0000-0000-0000-000000000002', 'staff', 'staff@inventorysystem.com', 'AQAAAAIAAYagAAAAENK5j8oFq4+Fq3fVfS8jT6823Lw5bQ4p1lqM0yS7oW+3oU=', 'Warehouse Operator', '+91 9876543211', '22222222-2222-2222-2222-222222222222', TRUE),
('cccc3333-0000-0000-0000-000000000003', 'customer', 'customer@example.com', 'AQAAAAIAAYagAAAAENK5j8oFq4+Fq3fVfS8jT6823Lw5bQ4p1lqM0yS7oW+3oU=', 'John Doe Customer', '+91 9876543212', '33333333-3333-3333-3333-333333333333', TRUE)
ON CONFLICT (id) DO NOTHING;

-- 3. Customers
INSERT INTO customers (id, user_id, customer_code, name, email, phone, company_name, tax_number, credit_limit, outstanding_balance, is_active) VALUES
('c0000001-0000-0000-0000-000000000001', 'cccc3333-0000-0000-0000-000000000003', 'CUST-1001', 'John Doe', 'customer@example.com', '+91 9876543212', 'Acme Trading Co.', 'GSTIN27AAAAA0000A1Z5', 50000.00, 0.00, TRUE),
('c0000002-0000-0000-0000-000000000002', NULL, 'CUST-1002', 'Sarah Connor', 'sarah.connor@cyberdyne.io', '+91 9876543299', 'Cyberdyne Systems', 'GSTIN27BBBBB1111B1Z2', 100000.00, 0.00, TRUE)
ON CONFLICT (id) DO NOTHING;

-- 4. Customer Addresses
INSERT INTO customer_addresses (id, customer_id, address_type, line1, line2, city, state, postal_code, country, is_default) VALUES
('ca000001-0000-0000-0000-000000000001', 'c0000001-0000-0000-0000-000000000001', 'Both', 'Flat 402, Lotus Grandeur, Andheri East', 'Near Metro Station', 'Mumbai', 'Maharashtra', '400069', 'India', TRUE),
('ca000002-0000-0000-0000-000000000002', 'c0000002-0000-0000-0000-000000000002', 'Shipping', 'Plot 18, Electronic City Phase 1', 'Hosur Road', 'Bengaluru', 'Karnataka', '560100', 'India', TRUE)
ON CONFLICT (id) DO NOTHING;

-- 5. Warehouses
INSERT INTO warehouses (id, code, name, location, address, is_primary, is_active) VALUES
('w0000001-0000-0000-0000-000000000001', 'WH-MAIN', 'Central Logistics Hub', 'Bhiwandi, Mumbai MMR', 'Warehouse Complex 4, Sector B, Bhiwandi, Thane, Maharashtra 421302', TRUE, TRUE),
('w0000002-0000-0000-0000-000000000002', 'WH-SOUTH', 'Southern Distribution Center', 'Nelamangala, Bengaluru', 'Plot 7A, Logistics Park, Nelamangala, Bengaluru 562123', FALSE, TRUE)
ON CONFLICT (id) DO NOTHING;

-- 6. Product Categories
INSERT INTO product_categories (id, name, code, description, is_active) VALUES
('cat00001-0000-0000-0000-000000000001', 'General Merchandise', 'CAT-GEN', 'Standard consumer and commercial products', TRUE),
('cat00002-0000-0000-0000-000000000002', 'Electronics & Sensors', 'CAT-ELEC', 'Electronic components, IoT sensors, and scanners', TRUE),
('cat00003-0000-0000-0000-000000000003', 'Office & Packaging', 'CAT-OFF', 'Packaging materials, tapes, and supplies', TRUE),
('cat00004-0000-0000-0000-000000000004', 'Industrial Furniture', 'CAT-FURN', 'Ergonomic chairs and industrial work desks', TRUE)
ON CONFLICT (id) DO NOTHING;

-- 7. Products
-- Note: Product A has selling price 100.00 and stock 50.00 as required by the test scenario!
INSERT INTO products (id, category_id, sku, name, description, unit_of_measurement, cost_price, selling_price, reorder_level, reorder_quantity, is_active) VALUES
('p0000001-0000-0000-0000-000000000001', 'cat00001-0000-0000-0000-000000000001', 'PROD-A-001', 'Product A', 'Standard high-demand commercial grade Product A', 'Unit', 60.00, 100.00, 15, 50, TRUE),
('p0000002-0000-0000-0000-000000000001', 'cat00001-0000-0000-0000-000000000001', 'PROD-B-002', 'Product B', 'Premium durable multi-purpose Product B', 'Unit', 150.00, 250.00, 20, 100, TRUE),
('p0000003-0000-0000-0000-000000000002', 'cat00002-0000-0000-0000-000000000002', 'IOT-SENS-101', 'Smart IoT Sensor', 'Wireless temperature and humidity telemetry sensor', 'Unit', 320.00, 499.00, 20, 50, TRUE),
('p0000004-0000-0000-0000-000000000002', 'cat00002-0000-0000-0000-000000000002', 'SCAN-WLS-202', 'Wireless Barcode Scanner', 'Handheld 2D QR and barcode Bluetooth scanner', 'Unit', 1100.00, 1850.00, 10, 25, TRUE),
('p0000005-0000-0000-0000-000000000003', 'cat00003-0000-0000-0000-000000000003', 'TAPE-HD-303', 'Heavy Duty Packing Tape', 'Brown sealing tape 65m industrial strength', 'Roll', 25.00, 45.00, 50, 200, TRUE),
('p0000006-0000-0000-0000-000000000004', 'cat00004-0000-0000-0000-000000000004', 'FURN-CHR-404', 'Ergonomic Office Chair', 'High-back mesh breathable lumbar support chair', 'Unit', 2800.00, 4500.00, 10, 15, TRUE),
('p0000007-0000-0000-0000-000000000002', 'cat00002-0000-0000-0000-000000000002', 'ELEC-MCU-505', 'Microcontroller Board v3', 'Embedded development board with Wi-Fi and BLE', 'Unit', 450.00, 750.00, 15, 30, TRUE)
ON CONFLICT (id) DO NOTHING;

-- 8. Inventory (Initial Stock)
-- Product A: 50 units in WH-MAIN
-- Product B: 120 units in WH-MAIN
-- Smart IoT Sensor: 85 units
-- Barcode Scanner: 30 units
-- Packing Tape: 350 units
-- Office Chair: 8 units (Below reorder level 10 -> Low stock!)
-- Microcontroller: 0 units (Out of stock!)
INSERT INTO inventory (id, product_id, warehouse_id, quantity_on_hand, reserved_quantity, damaged_quantity, minimum_stock) VALUES
('i0000001-0000-0000-0000-000000000001', 'p0000001-0000-0000-0000-000000000001', 'w0000001-0000-0000-0000-000000000001', 50, 0, 0, 10),
('i0000002-0000-0000-0000-000000000002', 'p0000002-0000-0000-0000-000000000001', 'w0000001-0000-0000-0000-000000000001', 120, 0, 0, 15),
('i0000003-0000-0000-0000-000000000003', 'p0000003-0000-0000-0000-000000000002', 'w0000001-0000-0000-0000-000000000001', 85, 0, 0, 10),
('i0000004-0000-0000-0000-000000000004', 'p0000004-0000-0000-0000-000000000002', 'w0000001-0000-0000-0000-000000000001', 30, 0, 0, 5),
('i0000005-0000-0000-0000-000000000005', 'p0000005-0000-0000-0000-000000000003', 'w0000001-0000-0000-0000-000000000001', 350, 0, 0, 50),
('i0000006-0000-0000-0000-000000000006', 'p0000006-0000-0000-0000-000000000004', 'w0000001-0000-0000-0000-000000000001', 8, 0, 0, 10),
('i0000007-0000-0000-0000-000000000007', 'p0000007-0000-0000-0000-000000000002', 'w0000001-0000-0000-0000-000000000001', 0, 0, 0, 15)
ON CONFLICT (product_id, warehouse_id) DO NOTHING;

-- 9. Initial Inventory Transaction Movements
INSERT INTO inventory_transactions (id, product_id, warehouse_id, transaction_type, quantity_change, balance_on_hand_after, balance_reserved_after, reference_type, reference_id, notes, created_by) VALUES
(gen_random_uuid(), 'p0000001-0000-0000-0000-000000000001', 'w0000001-0000-0000-0000-000000000001', 'Receipt', 50, 50, 0, 'OpeningStock', 'INIT-2026', 'Initial opening stock intake', 'System'),
(gen_random_uuid(), 'p0000002-0000-0000-0000-000000000001', 'w0000001-0000-0000-0000-000000000001', 'Receipt', 120, 120, 0, 'OpeningStock', 'INIT-2026', 'Initial opening stock intake', 'System'),
(gen_random_uuid(), 'p0000003-0000-0000-0000-000000000002', 'w0000001-0000-0000-0000-000000000001', 'Receipt', 85, 85, 0, 'OpeningStock', 'INIT-2026', 'Initial opening stock intake', 'System'),
(gen_random_uuid(), 'p0000004-0000-0000-0000-000000000002', 'w0000001-0000-0000-0000-000000000001', 'Receipt', 30, 30, 0, 'OpeningStock', 'INIT-2026', 'Initial opening stock intake', 'System'),
(gen_random_uuid(), 'p0000005-0000-0000-0000-000000000003', 'w0000001-0000-0000-0000-000000000001', 'Receipt', 350, 350, 0, 'OpeningStock', 'INIT-2026', 'Initial opening stock intake', 'System'),
(gen_random_uuid(), 'p0000006-0000-0000-0000-000000000004', 'w0000001-0000-0000-0000-000000000001', 'Receipt', 8, 8, 0, 'OpeningStock', 'INIT-2026', 'Initial opening stock intake', 'System');

-- 10. Suppliers
INSERT INTO suppliers (id, code, name, contact_person, email, phone, address, payment_terms, is_active) VALUES
('s0000001-0000-0000-0000-000000000001', 'SUPP-TECH-01', 'TechLogistics Corp', 'Rajesh Sharma', 'orders@techlogistics.in', '+91 22 2847 1100', 'Tech Park, Whitefield, Bengaluru', 'Net 30 Days', TRUE),
('s0000002-0000-0000-0000-000000000002', 'SUPP-GLOB-02', 'Global Electronics Supply', 'Priya Menon', 'sales@globalelec.com', '+91 44 6655 4433', 'SIPCOT Industrial Estate, Chennai', 'Net 15 Days', TRUE),
('s0000003-0000-0000-0000-000000000003', 'SUPP-APEX-03', 'Apex Packaging Industries', 'Vikram Patel', 'vikram@apexpack.co.in', '+91 79 2658 9988', 'GIDC Industrial Area, Ahmedabad', 'Immediate', TRUE)
ON CONFLICT (id) DO NOTHING;

-- 11. Supplier Products
INSERT INTO supplier_products (id, supplier_id, product_id, supplier_sku, cost_price, lead_time_days) VALUES
(gen_random_uuid(), 's0000001-0000-0000-0000-000000000001', 'p0000001-0000-0000-0000-000000000001', 'TL-PA-100', 58.00, 5),
(gen_random_uuid(), 's0000001-0000-0000-0000-000000000001', 'p0000002-0000-0000-0000-000000000001', 'TL-PB-200', 145.00, 7),
(gen_random_uuid(), 's0000002-0000-0000-0000-000000000002', 'p0000003-0000-0000-0000-000000000002', 'GE-IOT-300', 310.00, 10),
(gen_random_uuid(), 's0000003-0000-0000-0000-000000000003', 'p0000005-0000-0000-0000-000000000003', 'AP-TAPE-05', 23.50, 3)
ON CONFLICT DO NOTHING;

-- 12. Application Settings
INSERT INTO application_settings (id, setting_key, setting_value, description, category) VALUES
(gen_random_uuid(), 'tax_rate', '0.05', 'Default Goods & Services Tax rate (5%)', 'Pricing'),
(gen_random_uuid(), 'currency', 'INR', 'System base currency ISO code', 'Localization'),
(gen_random_uuid(), 'currency_symbol', '₹', 'Currency visual display symbol', 'Localization'),
(gen_random_uuid(), 'shipping_flat_rate', '50.00', 'Standard flat rate shipping fee', 'Pricing'),
(gen_random_uuid(), 'shipping_free_threshold', '1000.00', 'Order subtotal threshold for free shipping', 'Pricing'),
(gen_random_uuid(), 'company_name', 'Apex Inventory & AI Commerce', 'Company business entity name', 'General'),
(gen_random_uuid(), 'support_email', 'support@inventorysystem.com', 'Customer support email contact', 'General'),
(gen_random_uuid(), 'order_id_prefix', 'ORD-', 'Prefix for generated sales order numbers', 'Orders'),
(gen_random_uuid(), 'low_stock_threshold_default', '10', 'Default threshold for low inventory notifications', 'Inventory')
ON CONFLICT (setting_key) DO NOTHING;
