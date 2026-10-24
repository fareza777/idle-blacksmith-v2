# Verifikasi 0.4.0

Paket Windows dan Android diverifikasi pada 24 Oktober 2026 dengan Unity 6000.3.20f1.

| Pemeriksaan | Hasil |
| --- | --- |
| Logika permainan C# | 128 lulus, 0 gagal |
| Lima bab tanpa pemberian sumber daya tes | Selesai; 319 tindakan simulasi, 0 kekalahan |
| Alur Windows | 787 pemeriksaan lulus |
| Regresi tampilan Windows | 857 pemeriksaan lulus |
| Alur emulator Android API 36 | 787 pemeriksaan lulus |
| Regresi tampilan emulator Android API 36 | 851 pemeriksaan lulus |
| Aset dan penyimpanan Unity | 49 pemeriksaan lulus per target |
| Tinjauan independen setelah perbaikan | Tidak ada temuan material tersisa |

Pengujian native mencakup tujuan aktif, hasil tempa dan pemakaian perlengkapan, kelanjutan hadiah, pembukaan bab, reservasi/penyerahan pesanan, pemulihan setelah kalah, penempaan manual, ukuran teks besar, dan layar portrait ringkas. Android diuji otomatis pada emulator API 36 beresolusi 1080 x 2400. Jumlah pemeriksaan tampilan mencakup label sementara sehingga dapat berbeda antar-target.

Profil tes dipisahkan dari penyimpanan permainan biasa. APK 0.4.0 kemudian berhasil dipasang sebagai pembaruan dan dibuka pada HP Android yang terhubung; ini adalah pemeriksaan pemasangan/pembukaan, bukan pengujian otomatis lengkap pada HP tersebut.

Tes logika juga mencakup pemuatan simpan lama, pemenuhan pesanan sekali saja, bahan dan hadiah yang tidak terduplikasi, perlindungan barang dari penjual, persiapan hero baru untuk wilayah lanjut, serta batas produksi otomatis.

Ilustrasi workshop pada README diambil dari pengujian native versi ini. Bukti mentah dan paket hasil build disimpan lokal di folder QA dan Builds, terpisah dari sumber Git.
