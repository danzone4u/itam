-- =========================================================
-- Script untuk membuat Dedicated User SQL Server untuk ITAM
-- =========================================================

USE [master];
GO

-- 1. Buat Login baru (Ganti 'PasswordAman123!' dengan password yang sangat kuat)
CREATE LOGIN [itam_user] WITH PASSWORD = N'PasswordAman123!', 
    DEFAULT_DATABASE = [itamDB], 
    CHECK_EXPIRATION = OFF, 
    CHECK_POLICY = ON;
GO

-- 2. Pindah ke database ITAM
USE [itamDB];
GO

-- 3. Buat User di database itamDB yang terhubung dengan Login di atas
CREATE USER [itam_user] FOR LOGIN [itam_user];
GO

-- 4. Berikan akses read dan write data
ALTER ROLE [db_datareader] ADD MEMBER [itam_user];
ALTER ROLE [db_datawriter] ADD MEMBER [itam_user];
GO

-- 5. (Opsional tapi disarankan) Berikan izin untuk mengeksekusi Stored Procedure 
-- jika suatu saat Entity Framework menggunakannya
GRANT EXECUTE TO [itam_user];
GO

-- 6. (Penting untuk BackupController) Berikan izin untuk Backup & Restore secara spesifik
-- ke user itam_user agar fitur Backup di dalam aplikasi tetap bisa berjalan.
GRANT BACKUP DATABASE TO [itam_user];
GO
