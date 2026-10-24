# Scorecard desain

Skala 0–3 per prinsip, total **12/30**. Ini penilaian heuristik terhadap antarmuka yang diperiksa, bukan hasil tes usability atau skor keseluruhan kualitas permainan. Skor memakai kondisi paling lemah yang representatif, bukan rata-rata layar. Bukti gabungan: [01-evidence.md](01-evidence.md).

1. **Good design is innovative — 1/3**  
   Evidence: [E4](01-evidence.md#e4--first-session-and-item-purpose), [E5](01-evidence.md#e5--existing-gameplay-depth-and-presentation): produksi, timing strike, equip, dan ekspedisi tersambung dalam pola idle/RPG yang umum.  
   Justification: integrasi punya identitas, tetapi audit ini tidak membuktikan pola interaksi baru atau perbaikan yang sudah tervalidasi; benchmark lima produk sejenis tidak dilakukan.

2. **Good design makes a product useful — 1/3**  
   Evidence: [E3](01-evidence.md#e3--navigation-and-workflow) dan laporan struktur: dari inventory, equip → dismiss → world → region → depart membutuhkan perpindahan konteks; jalur yang berawal di Heroes menambah pembukaan ulang layar.  
   Justification: tugas dapat diselesaikan, tetapi persiapan ekspedisi memerlukan jalan memutar yang tidak membantu keputusan pemain.

3. **Good design is aesthetic — 1/3**  
   Evidence: [E1](01-evidence.md#e1--typography-and-scale), [E2](01-evidence.md#e2--visual-focus-and-affordances): palet/art konsisten, tetapi 16 ukuran teks didominasi 20–33 unit dalam root 1080; nama tugas, status, navigasi, dan detail dunia saling bersaing.  
   Justification: keterbacaan teks utama merupakan pelanggaran besar dalam hierarki visual, walaupun aset dan palet memiliki sistem yang dapat dipertahankan.

4. **Good design makes a product understandable — 1/3**  
   Evidence: [E2](01-evidence.md#e2--visual-focus-and-affordances), [E3](01-evidence.md#e3--navigation-and-workflow), [E4](01-evidence.md#e4--first-session-and-item-purpose): ikon World/Settings berulang, modal kehilangan konteks asal, dan welcome mengajarkan penjualan sebelum menjelaskan retensi perlengkapan.  
   Justification: beberapa kontrol dan urutan aktivitas perlu penjelasan; tidak diberi 0 karena aksi Mine dan label tab tetap dapat dikenali dari screenshot. Pemahaman pemain baru belum diuji.

5. **Good design is unobtrusive — 1/3**  
   Evidence: [E2](01-evidence.md#e2--visual-focus-and-affordances), [E8](01-evidence.md#e8--weight-and-attention): art latar, empat badge, objective, clock, peta, dock, dan tab berbagi perhatian; beberapa gerak ambient terus berjalan.  
   Justification: suasana dunia mendukung tema tetapi bersaing dengan informasi produksi dan keputusan utama yang kecil.

6. **Good design is honest — 1/3**  
   Evidence: [E6](01-evidence.md#e6--honest-labels-and-state-accuracy): penjualan dan komisi mengklaim reputation tanpa mutasi reputation; deskripsi smith dua ingot tidak berlaku untuk resep lain; footer ekspedisi aktif tetap muncul saat victory.  
   Justification: terdapat lebih dari satu ketidaksesuaian janji/label dengan perilaku aktual. Tidak ditemukan bukti alur pembelian manipulatif dalam cakupan.

7. **Good design is long-lasting — 3/3**  
   Evidence: [E2](01-evidence.md#e2--visual-focus-and-affordances): pixel art bengkel/hero/peralatan dan palet navy–cream–gold menggunakan bahasa visual yang konsisten, tanpa ketergantungan pada efek UI tren yang dominan.  
   Justification: identitas visual cukup mandiri untuk dipertahankan selama perombakan struktur; ini pertimbangan editorial, bukan prediksi terukur tiga tahun.

8. **Good design is thorough down to the last detail — 1/3**  
   Evidence: [E7](01-evidence.md#e7--states-and-access) dan laporan struktur: empty/error/success/disabled tersedia, tetapi fokus keyboard tidak disusun; beberapa kontrol tetap aktif ketika aksi ditolak core, termasuk equip/hero switch ketika ekspedisi aktif.  
   Justification: ada dua sampai tiga state/interaksi yang hilang atau tidak konsisten. Loading tidak dipenalti sebagai hilang karena layar dibangun sinkron dan kebutuhan loading belum dibuktikan.

9. **Good design is environmentally friendly — 1/3**  
   Evidence: [E8](01-evidence.md#e8--weight-and-attention), [bukti bobot](evidence-weight.md): target 60 fps, sedikitnya delapan elemen ambient berubah, tidak ada pengaturan gerak, pengukuran baterai/cold start belum tersedia.  
   Justification: dimensi sumber daya/perhatian perlu perbaikan karena motion selalu aktif. **Adaptasi native:** ambang bobot JavaScript, browser network, dan prefers-reduced-motion pada rubrik web tidak dipaksakan ke build Unity; ukuran APK tidak dipakai sebagai proxy baterai.

10. **Good design is as little design as possible — 1/3**  
    Evidence: [E2](01-evidence.md#e2--visual-focus-and-affordances), [E3](01-evidence.md#e3--navigation-and-workflow): objective/rank/clock/shortcut dan akses berulang membagi fokus; daftar masa depan mengambil ruang baca yang sama dengan tujuan kini.  
    Justification: setidaknya tiga kelompok informasi/entry dapat diringkas atau dipindahkan ke konteks terkait. Hotspot langsung dan semua dismissal tidak otomatis dianggap harus dihapus.

Total: `1 + 1 + 1 + 1 + 1 + 1 + 3 + 1 + 1 + 1 = 12 / 30`.

Aturan verdict: total di bawah 20 → **REDESIGN**. Cakupannya UI utama, navigasi, dan onboarding; identitas pixel art serta sistem inti dipertahankan.
