// using Internal;
using System.Runtime.CompilerServices;
using System;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;

// added by 0BoO
using System.Text;
using OfficeOpenXml;
using OfficeOpenXml.FormulaParsing;
using System.Threading.Tasks;

namespace surveillance_system
{
    public partial class Program
    {
        //static int randSeed = 1734;
        //public static Random rand = new Random(randSeed); // modified by 0boo 23-01-27
        static string Sim_ID = "230713_TEST";
        static int numSim = 5;
        static int initRandSeed = 1731;
        static int[] randSeedList = new int [numSim];

        const int param_N_CCTV = 10;
        const int param_N_PED = 10;

        public static Random rand; // modified by 0boo 23-01-27

        public static CCTV[] cctvs;
        public static Pedestrian[] peds;

        // Configuration: simulation time
        const double aUnitTime = 100 * 0.001; // (sec) default value: 100 ms
        public static Road road = new Road();

        const bool On_Visualization = false;
        const bool Opt_Observation = false;
        const bool Opt_Demo = false;
        const bool Opt_Log = false; // to get log of events

        /* --------------------------------------
         * 추적 여부 검사 함수
        -------------------------------------- */
        static int[] checkDetection(int N_CCTV, int N_Ped)
        {

            int[] returnArr = new int[N_Ped]; // 반환할 탐지 결과 (1: 탐지  0: 거리상 미탐지  -1: 방향 미스)
            
            
            // 거리 검사(최소 ppm 기준)
            int[,] candidate_detected_ped_h = new int[N_CCTV, N_Ped];
            int[,] candidate_detected_ped_v = new int[N_CCTV, N_Ped];

            // 거리 검사(Effective distance set 기준)
            int[,] candidate_detected_ped1 = new int[N_CCTV, N_Ped]; // shorter than Max Distance(Eff_dist_To)?
            int[,] candidate_detected_ped2 = new int[N_CCTV, N_Ped]; // longer than Blinded Distance(Eff_dist_From)? 

            for (int i = 0; i < N_CCTV; i++)
            {
                
                for (int j = 0; j < N_Ped; j++)
                {   
                    for (int k = 0; k < Convert.ToInt32(peds[j].Spatial_Resolution.GetLength(1)); k++)
                    {
                        peds[j].Spatial_Resolution[i,k] = 0;
                    }
                    

                    double dist_h1 = Math
                            .Sqrt(Math.Pow(cctvs[i].X - peds[j].Pos_H1[0], 2) +
                            Math.Pow(cctvs[i].Y - peds[j].Pos_H1[1], 2));
                    double dist_h2 = Math
                            .Sqrt(Math.Pow(cctvs[i].X - peds[j].Pos_H2[0], 2) +
                            Math.Pow(cctvs[i].Y - peds[j].Pos_H2[1], 2));
                    double dist_v1 = Math
                            .Sqrt(Math.Pow(cctvs[i].X - peds[j].Pos_V1[0], 2) +
                            Math.Pow(cctvs[i].Z - peds[j].Pos_V1[1], 2));
                    double dist_v2 = Math
                            .Sqrt(Math.Pow(cctvs[i].X - peds[j].Pos_V2[0], 2) +
                            Math.Pow(cctvs[i].Z - peds[j].Pos_V2[1], 2)) ;

                    // (23-02-07) Now, ignore bleow two foreach statements
                    foreach (double survdist_h in cctvs[i].SurvDist_H)
                    {
                        if (dist_h1 <= survdist_h*100*10 && dist_h2 <= survdist_h * 100 * 10)
                        {
                            candidate_detected_ped_h[i, j] = 1;
                        }
                    }
                    foreach (double survdist_v in cctvs[i].SurvDist_V)
                    {
                        if (dist_v1 <= survdist_v * 100 * 10 && dist_v2 <= survdist_v * 100 * 10)
                        {
                            candidate_detected_ped_v[i, j] = 1;
                        }
                    }

                    if ( dist_h1 <= cctvs[i].Eff_Dist_To && dist_h2 <= cctvs[i].Eff_Dist_To)
                    {
                        candidate_detected_ped1[i,j] = 1;
                    }

                    if (dist_h1 >= cctvs[i].Eff_Dist_From && dist_h2 >= cctvs[i].Eff_Dist_From)
                    {
                        candidate_detected_ped2[i, j] = 1;
                    }
                    // if (cctvs[i].isPedInEffDist(peds[j])) {
                    //   candidate_detected_ped_h[i, j] = 1;
                    //   candidate_detected_ped_v[i, j] = 1;
                    // }

                    // candidate_detected_ped_h[i, j] = 1;
                    // candidate_detected_ped_v[i, j] = 1;
                    if (candidate_detected_ped1[i, j] == 1 && candidate_detected_ped2[i, j] == 1)
                    {
                        peds[j].Spatial_Resolution[i, 0] = -1;
                        peds[j].Spatial_Resolution[i, 1] = (cctvs[i].WD * dist_h1) / (cctvs[i].Focal_Length * cctvs[i].imW);
                        peds[j].Spatial_Resolution[i, 2] = (cctvs[i].WD * dist_h2) / (cctvs[i].Focal_Length * cctvs[i].imW);
                        peds[j].Spatial_Resolution[i, 3] = (cctvs[i].HE * dist_v1) / (cctvs[i].Focal_Length * cctvs[i].imH);
                        peds[j].Spatial_Resolution[i, 4] = (cctvs[i].HE * dist_v2) / (cctvs[i].Focal_Length * cctvs[i].imH);

                        peds[j].Spatial_Resolution[i, 5] = peds[j].W / Math.Min(peds[j].Spatial_Resolution[i, 1], peds[j].Spatial_Resolution[i, 2]);
                        peds[j].Spatial_Resolution[i, 6] = peds[j].W / Math.Max(peds[j].Spatial_Resolution[i, 1], peds[j].Spatial_Resolution[i, 2]);
                        peds[j].Spatial_Resolution[i, 7] = peds[j].H / Math.Min(peds[j].Spatial_Resolution[i, 3], peds[j].Spatial_Resolution[i, 4]);
                        peds[j].Spatial_Resolution[i, 8] = peds[j].H / Math.Max(peds[j].Spatial_Resolution[i, 3], peds[j].Spatial_Resolution[i, 4]);

                        peds[j].Spatial_Resolution[i, 9] = Math.Min(peds[j].Spatial_Resolution[i, 5], peds[j].Spatial_Resolution[i, 6]) * Math.Min(peds[j].Spatial_Resolution[i, 7], peds[j].Spatial_Resolution[i, 8]);
                        peds[j].Spatial_Resolution[i, 10] = Math.Max(peds[j].Spatial_Resolution[i, 5], peds[j].Spatial_Resolution[i, 6]) * Math.Max(peds[j].Spatial_Resolution[i, 7], peds[j].Spatial_Resolution[i, 8]);
                    }   
                }
            }



            // return returnArr;

            // 각 CCTV의 보행자 탐지횟수 계산
            int[] cctv_detecting_cnt = new int[N_CCTV];
            int[] cctv_missing_cnt = new int[N_CCTV];

            int[,] missed_map_h = new int[N_CCTV, N_Ped];
            int[,] missed_map_v = new int[N_CCTV, N_Ped];

            int[,] detected_map = new int[N_CCTV, N_Ped];

            // 각도 검사 
            for (int i = 0; i < N_CCTV; i++)
            {
                double cosine_H_AOV = Math.Cos(cctvs[i].H_AOV / 2);
                double cosine_V_AOV = Math.Cos(cctvs[i].V_AOV / 2);

                for (int j = 0; j < N_Ped; j++)
                {

                    // 거리상 미탐지면 넘어감 --> (23-02-07) blocked
                    //if (candidate_detected_ped_h[i, j] != 1 || candidate_detected_ped_v[i, j] != 1)
                    //{                      
                    //    continue;
                    //}

                    if (candidate_detected_ped1[i, j] != 1 || candidate_detected_ped2[i, j] != 1)
                    {
                        continue;
                    }

                    int h_detected = -1;
                    int v_detected = -1;

                    // 거리가 범위 내이면 --> (23-02-07) cctv와 개체 간의 거리가 유효 거리 범위이면 
                    if (candidate_detected_ped1[i, j] == 1 && candidate_detected_ped2[i, j] == 1)//if (candidate_detected_ped_h[i, j] == 1)
                    {
                        // len equals Dist
                        returnArr[j] = (returnArr[j] == 1 ? 1 : -1); // (23-04-03)

                        int len = cctvs[i].H_FOV.X0.GetLength(0);
                        double[] A = { cctvs[i].H_FOV.X0[len - 1] - cctvs[i].X, cctvs[i].H_FOV.Y0[len - 1] - cctvs[i].Y };
                        double[] B = { peds[j].Pos_H1[0] - cctvs[i].X, peds[j].Pos_H1[1] - cctvs[i].Y };
                        double cosine_PED_h1 = InnerProduct(A, B) / (Norm(A) * Norm(B));

                        B[0] = peds[j].Pos_H2[0] - cctvs[i].X;
                        B[1] = peds[j].Pos_H2[1] - cctvs[i].Y;
                        double cosine_PED_h2 = InnerProduct(A, B) / (Norm(A) * Norm(B));

                        // horizontal 각도 검사 
                        if (cosine_PED_h1 >= cosine_H_AOV && cosine_PED_h2 >= cosine_H_AOV)
                        {
                            //감지 됨
                            h_detected = 1;
                        }
                        else
                        {
                            h_detected = 0;
                        }
                    }

                    // vertical  각도 검사 --> (23-02-07) 비효율적이지만, 검증 및 디버깅을 위해 남겨둠
                    //                     --> (23-04-03) 필요한 코드로 판별됨
                    //                                    horizontal domain 상에서 유효 거리 이내여도,
                    //                                    vertical domain 측면에서 target은 감시 영역을 벗어날 수 있음.
                    if (candidate_detected_ped1[i, j] == 1 && candidate_detected_ped2[i, j] == 1) //if (candidate_detected_ped_v[i, j] == 1)
                    {
                        // Surv_SYS_v210202.m [line 260]
                        /*         
                          if ismember(j, Candidates_Detected_PED_V1)
                          A = [CCTV(i).V_FOV_X0(1,:); CCTV(i).V_FOV_Z0(1,:)] - [CCTV(i).X; CCTV(i).Z];
                          B = [PED(j).Pos_V1(1); PED(j).Pos_V1(2)] - [CCTV(i).X; CCTV(i).Z]; 
                        */
                        returnArr[j] = (returnArr[j] == 1 ? 1 : -1); // (23-04-03)

                        int len = cctvs[i].V_FOV.X0.GetLength(0);
                        double[] A = { cctvs[i].V_FOV.X0[len - 1] - cctvs[i].X, cctvs[i].V_FOV.Z0[len - 1] - cctvs[i].Z };
                        double[] B = { peds[j].Pos_V1[0] - cctvs[i].X, peds[j].Pos_V1[1] - cctvs[i].Z };
                        double cosine_PED_v1 = InnerProduct(A, B) / (Norm(A) * Norm(B));

                        B[0] = peds[j].Pos_V2[0] - cctvs[i].X;
                        B[1] = peds[j].Pos_V2[1] - cctvs[i].Z;
                        double cosine_PED_v2 = InnerProduct(A, B) / (Norm(A) * Norm(B));

                        if (cosine_PED_v1 >= cosine_V_AOV && cosine_PED_v2 >= cosine_V_AOV)
                        {
                            //감지 됨
                            v_detected = 1;
                        }
                        else
                        {
                            v_detected = 0;
                            
                        }
                    }

                  
                    if (h_detected == 1 && v_detected == 1)
                    {
                        detected_map[i, j] = 1;
                        // 각 CCTV[i]의 보행자 탐지 횟수 증가
                        cctv_detecting_cnt[i]++;

                        returnArr[j] = 1;
                        // 220407
                        cctvs[i].detectedPedIndex.Add(j);
                        peds[j].Spatial_Resolution[i, 0] = 1;
                    }
                    // 방향 미스 (h or v 중 하나라도 방향이 맞지 않는 경우)
                    else // cctv[i]가 보행자[j]를 h or v 탐지 실패 여부 추가
                    {
                        cctv_missing_cnt[i]++;
                        
                        if(h_detected == 0) missed_map_h[i, j] = 1;

                        if(v_detected == 0) missed_map_v[i, j] = 1;

                        //if (returnArr[j] == 1) Console.WriteLine("unexpected value in returnArr at checkDetection()!");

                        //returnArr[j] = (returnArr[j] == 1 ? 1 : -1); // (note_230328) returnArr[j] = -1; 로 대체하면 어떻게 되는가? 
                                                                     // (note_230328) 다른 CCTV에 이미 탐지된 j를 탐지되지 않은 것으로 처리되지 않도록 하기 위함.

                        

                        /*
                        if(h_detected != 1)
                        {
                            Console.WriteLine("[{0}] horizontal 감지 못함", h_detected);
                        }
                        else if(v_detected != 1)
                        {
                            Console.WriteLine("[{0}] vertical 감지 못함 ", v_detected);
                        }
                        */
                    }


                } // 탐지 여부 계산 완료
            }



            // 여기부턴 h or v 각각 분석
            // 각 cctv는 h, v 축에서 얼마나 많이 놓쳤나?
            int[] cctv_missing_count_h = new int[N_CCTV];
            int[] cctv_missing_count_v = new int[N_CCTV];

            for(int i = 0 ; i < N_CCTV; i++)
            for(int j = 0 ; j < N_Ped; j++)
            {
                cctv_missing_count_h[i] += missed_map_h[i, j];
                cctv_missing_count_v[i] += missed_map_v[i, j];
            }
            // 보행자를 탐지한 cctv 수
            int[] detecting_cctv_cnt = new int[N_Ped];
            // 보행자를 탐지하지 못한 cctv 수
            int[] missing_cctv_cnt = new int[N_Ped];

            //Console.WriteLine("=== 성공 ====");
            // detection 결과 출력 
            for (int i = 0; i < N_CCTV; i++)
            {
                for (int j = 0; j < N_Ped; j++)
                {
                    if (detected_map[i, j] == 1)
                    {
                        detecting_cctv_cnt[j]++;
                    }
                    else
                    {
                        missing_cctv_cnt[j]++;
                    }
                }
            }

// for time check
            // Console.WriteLine("---------------------------------");
            // Console.WriteLine("   성공  ||   실패  ");
            // for (int i = 0; i < N_Ped; i++)
            // {
            //     if (detecting_cctv_cnt[i] == 0)
            //     {
            //         Console.WriteLine("         ||   ped{0} ", i + 1);
            //     }
            //     else
            //     {
            //         Console.WriteLine("ped{0} ", i + 1);
            //     }
            // }
            // Console.WriteLine("---------------------------------");


            return returnArr;
        }

        static int[] checkDetection_ParFor(int N_CCTV, int N_Ped)
        {
            int[] returnArr = new int[N_Ped]; // 반환할 탐지 결과 (1: 탐지  0: 거리상 미탐지  -1: 방향 미스)


            // 거리 검사(최소 ppm 기준)
            int[,] candidate_detected_ped_h = new int[N_CCTV, N_Ped];
            int[,] candidate_detected_ped_v = new int[N_CCTV, N_Ped];

            // 거리 검사(Effective distance set 기준)
            int[,] candidate_detected_ped1 = new int[N_CCTV, N_Ped]; // shorter than Max Distance(Eff_dist_To)?
            int[,] candidate_detected_ped2 = new int[N_CCTV, N_Ped]; // longer than Blinded Distance(Eff_dist_From)? 

            Parallel.For(0, N_CCTV, i =>
            {

                for (int j = 0; j < N_Ped; j++)
                {
                    for (int k = 0; k < Convert.ToInt32(peds[j].Spatial_Resolution.GetLength(1)); k++)
                    {
                        peds[j].Spatial_Resolution[i, k] = 0;
                    }


                    double dist_h1 = Math
                            .Sqrt(Math.Pow(cctvs[i].X - peds[j].Pos_H1[0], 2) +
                            Math.Pow(cctvs[i].Y - peds[j].Pos_H1[1], 2));
                    double dist_h2 = Math
                            .Sqrt(Math.Pow(cctvs[i].X - peds[j].Pos_H2[0], 2) +
                            Math.Pow(cctvs[i].Y - peds[j].Pos_H2[1], 2));
                    double dist_v1 = Math
                            .Sqrt(Math.Pow(cctvs[i].X - peds[j].Pos_V1[0], 2) +
                            Math.Pow(cctvs[i].Z - peds[j].Pos_V1[1], 2));
                    double dist_v2 = Math
                            .Sqrt(Math.Pow(cctvs[i].X - peds[j].Pos_V2[0], 2) +
                            Math.Pow(cctvs[i].Z - peds[j].Pos_V2[1], 2));

                    // (23-02-07) Now, ignore bleow two foreach statements
                    foreach (double survdist_h in cctvs[i].SurvDist_H)
                    {
                        if (dist_h1 <= survdist_h * 100 * 10 && dist_h2 <= survdist_h * 100 * 10)
                        {
                            candidate_detected_ped_h[i, j] = 1;
                        }
                    }
                    foreach (double survdist_v in cctvs[i].SurvDist_V)
                    {
                        if (dist_v1 <= survdist_v * 100 * 10 && dist_v2 <= survdist_v * 100 * 10)
                        {
                            candidate_detected_ped_v[i, j] = 1;
                        }
                    }

                    if (dist_h1 <= cctvs[i].Eff_Dist_To && dist_h2 <= cctvs[i].Eff_Dist_To)
                    {
                        candidate_detected_ped1[i, j] = 1;
                    }

                    if (dist_h1 >= cctvs[i].Eff_Dist_From && dist_h2 >= cctvs[i].Eff_Dist_From)
                    {
                        candidate_detected_ped2[i, j] = 1;
                    }
                    // if (cctvs[i].isPedInEffDist(peds[j])) {
                    //   candidate_detected_ped_h[i, j] = 1;
                    //   candidate_detected_ped_v[i, j] = 1;
                    // }

                    // candidate_detected_ped_h[i, j] = 1;
                    // candidate_detected_ped_v[i, j] = 1;
                    if (candidate_detected_ped1[i, j] == 1 && candidate_detected_ped2[i, j] == 1)
                    {
                        peds[j].Spatial_Resolution[i, 0] = -1;
                        peds[j].Spatial_Resolution[i, 1] = (cctvs[i].WD * dist_h1) / (cctvs[i].Focal_Length * cctvs[i].imW);
                        peds[j].Spatial_Resolution[i, 2] = (cctvs[i].WD * dist_h2) / (cctvs[i].Focal_Length * cctvs[i].imW);
                        peds[j].Spatial_Resolution[i, 3] = (cctvs[i].HE * dist_v1) / (cctvs[i].Focal_Length * cctvs[i].imH);
                        peds[j].Spatial_Resolution[i, 4] = (cctvs[i].HE * dist_v2) / (cctvs[i].Focal_Length * cctvs[i].imH);

                        peds[j].Spatial_Resolution[i, 5] = peds[j].W / Math.Min(peds[j].Spatial_Resolution[i, 1], peds[j].Spatial_Resolution[i, 2]);
                        peds[j].Spatial_Resolution[i, 6] = peds[j].W / Math.Max(peds[j].Spatial_Resolution[i, 1], peds[j].Spatial_Resolution[i, 2]);
                        peds[j].Spatial_Resolution[i, 7] = peds[j].H / Math.Min(peds[j].Spatial_Resolution[i, 3], peds[j].Spatial_Resolution[i, 4]);
                        peds[j].Spatial_Resolution[i, 8] = peds[j].H / Math.Max(peds[j].Spatial_Resolution[i, 3], peds[j].Spatial_Resolution[i, 4]);

                        peds[j].Spatial_Resolution[i, 9] = Math.Min(peds[j].Spatial_Resolution[i, 5], peds[j].Spatial_Resolution[i, 6]) * Math.Min(peds[j].Spatial_Resolution[i, 7], peds[j].Spatial_Resolution[i, 8]);
                        peds[j].Spatial_Resolution[i, 10] = Math.Max(peds[j].Spatial_Resolution[i, 5], peds[j].Spatial_Resolution[i, 6]) * Math.Max(peds[j].Spatial_Resolution[i, 7], peds[j].Spatial_Resolution[i, 8]);
                    }
                }
            });



            // return returnArr;

            // 각 CCTV의 보행자 탐지횟수 계산
            int[] cctv_detecting_cnt = new int[N_CCTV];
            int[] cctv_missing_cnt = new int[N_CCTV];

            int[,] missed_map_h = new int[N_CCTV, N_Ped];
            int[,] missed_map_v = new int[N_CCTV, N_Ped];

            int[,] detected_map = new int[N_CCTV, N_Ped];

            // 각도 검사 
            Parallel.For(0, N_CCTV, i =>
            {
                double cosine_H_AOV = Math.Cos(cctvs[i].H_AOV / 2);
                double cosine_V_AOV = Math.Cos(cctvs[i].V_AOV / 2);

                for (int j = 0; j < N_Ped; j++)
                {

                    // 거리상 미탐지면 넘어감 --> (23-02-07) blocked
                    //if (candidate_detected_ped_h[i, j] != 1 || candidate_detected_ped_v[i, j] != 1)
                    //{                      
                    //    continue;
                    //}

                    if (candidate_detected_ped1[i, j] != 1 || candidate_detected_ped2[i, j] != 1)
                    {
                        continue;
                    }

                    int h_detected = -1;
                    int v_detected = -1;

                    // 거리가 범위 내이면 --> (23-02-07) cctv와 개체 간의 거리가 유효 거리 범위이면 
                    if (candidate_detected_ped1[i, j] == 1 && candidate_detected_ped2[i, j] == 1)//if (candidate_detected_ped_h[i, j] == 1)
                    {
                        // len equals Dist
                        returnArr[j] = (returnArr[j] == 1 ? 1 : -1); // (23-04-03)

                        int len = cctvs[i].H_FOV.X0.GetLength(0);
                        double[] A = { cctvs[i].H_FOV.X0[len - 1] - cctvs[i].X, cctvs[i].H_FOV.Y0[len - 1] - cctvs[i].Y };
                        double[] B = { peds[j].Pos_H1[0] - cctvs[i].X, peds[j].Pos_H1[1] - cctvs[i].Y };
                        double cosine_PED_h1 = InnerProduct(A, B) / (Norm(A) * Norm(B));

                        B[0] = peds[j].Pos_H2[0] - cctvs[i].X;
                        B[1] = peds[j].Pos_H2[1] - cctvs[i].Y;
                        double cosine_PED_h2 = InnerProduct(A, B) / (Norm(A) * Norm(B));

                        // horizontal 각도 검사 
                        if (cosine_PED_h1 >= cosine_H_AOV && cosine_PED_h2 >= cosine_H_AOV)
                        {
                            //감지 됨
                            h_detected = 1;
                        }
                        else
                        {
                            h_detected = 0;
                        }
                    }

                    // vertical  각도 검사 --> (23-02-07) 비효율적이지만, 검증 및 디버깅을 위해 남겨둠
                    //                     --> (23-04-03) 필요한 코드로 판별됨
                    //                                    horizontal domain 상에서 유효 거리 이내여도,
                    //                                    vertical domain 측면에서 target은 감시 영역을 벗어날 수 있음.
                    if (candidate_detected_ped1[i, j] == 1 && candidate_detected_ped2[i, j] == 1) //if (candidate_detected_ped_v[i, j] == 1)
                    {
                        // Surv_SYS_v210202.m [line 260]
                        /*         
                          if ismember(j, Candidates_Detected_PED_V1)
                          A = [CCTV(i).V_FOV_X0(1,:); CCTV(i).V_FOV_Z0(1,:)] - [CCTV(i).X; CCTV(i).Z];
                          B = [PED(j).Pos_V1(1); PED(j).Pos_V1(2)] - [CCTV(i).X; CCTV(i).Z]; 
                        */
                        returnArr[j] = (returnArr[j] == 1 ? 1 : -1); // (23-04-03)

                        int len = cctvs[i].V_FOV.X0.GetLength(0);
                        double[] A = { cctvs[i].V_FOV.X0[len - 1] - cctvs[i].X, cctvs[i].V_FOV.Z0[len - 1] - cctvs[i].Z };
                        double[] B = { peds[j].Pos_V1[0] - cctvs[i].X, peds[j].Pos_V1[1] - cctvs[i].Z };
                        double cosine_PED_v1 = InnerProduct(A, B) / (Norm(A) * Norm(B));

                        B[0] = peds[j].Pos_V2[0] - cctvs[i].X;
                        B[1] = peds[j].Pos_V2[1] - cctvs[i].Z;
                        double cosine_PED_v2 = InnerProduct(A, B) / (Norm(A) * Norm(B));

                        if (cosine_PED_v1 >= cosine_V_AOV && cosine_PED_v2 >= cosine_V_AOV)
                        {
                            //감지 됨
                            v_detected = 1;
                        }
                        else
                        {
                            v_detected = 0;

                        }
                    }


                    if (h_detected == 1 && v_detected == 1)
                    {
                        detected_map[i, j] = 1;
                        // 각 CCTV[i]의 보행자 탐지 횟수 증가
                        cctv_detecting_cnt[i]++;

                        returnArr[j] = 1;
                        // 220407
                        cctvs[i].detectedPedIndex.Add(j);
                        peds[j].Spatial_Resolution[i, 0] = 1;
                    }
                    // 방향 미스 (h or v 중 하나라도 방향이 맞지 않는 경우)
                    else // cctv[i]가 보행자[j]를 h or v 탐지 실패 여부 추가
                    {
                        cctv_missing_cnt[i]++;

                        if (h_detected == 0) missed_map_h[i, j] = 1;

                        if (v_detected == 0) missed_map_v[i, j] = 1;

                        //if (returnArr[j] == 1) Console.WriteLine("unexpected value in returnArr at checkDetection()!");

                        //returnArr[j] = (returnArr[j] == 1 ? 1 : -1); // (note_230328) returnArr[j] = -1; 로 대체하면 어떻게 되는가? 
                        // (note_230328) 다른 CCTV에 이미 탐지된 j를 탐지되지 않은 것으로 처리되지 않도록 하기 위함.



                        /*
                        if(h_detected != 1)
                        {
                            Console.WriteLine("[{0}] horizontal 감지 못함", h_detected);
                        }
                        else if(v_detected != 1)
                        {
                            Console.WriteLine("[{0}] vertical 감지 못함 ", v_detected);
                        }
                        */
                    }


                } // 탐지 여부 계산 완료
            });



            // 여기부턴 h or v 각각 분석
            // 각 cctv는 h, v 축에서 얼마나 많이 놓쳤나?
            int[] cctv_missing_count_h = new int[N_CCTV];
            int[] cctv_missing_count_v = new int[N_CCTV];

            for (int i = 0; i < N_CCTV; i++)
                for (int j = 0; j < N_Ped; j++)
                {
                    cctv_missing_count_h[i] += missed_map_h[i, j];
                    cctv_missing_count_v[i] += missed_map_v[i, j];
                }
            // 보행자를 탐지한 cctv 수
            int[] detecting_cctv_cnt = new int[N_Ped];
            // 보행자를 탐지하지 못한 cctv 수
            int[] missing_cctv_cnt = new int[N_Ped];

            //Console.WriteLine("=== 성공 ====");
            // detection 결과 출력 
            for (int i = 0; i < N_CCTV; i++)
            {
                for (int j = 0; j < N_Ped; j++)
                {
                    if (detected_map[i, j] == 1)
                    {
                        detecting_cctv_cnt[j]++;
                    }
                    else
                    {
                        missing_cctv_cnt[j]++;
                    }
                }
            }


            return returnArr;
        }
            static void Main(string[] args)
        {

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            /*------------------------------------------------------------------------
              % note 1) To avoid confusing, all input parameters for a distance has a unit as a milimeter
            -------------------------------------------------------------------------*/
            randSeedList[0] = initRandSeed;
            for (int i = 1; i < numSim; i++)
            {
                randSeedList[i] = randSeedList[i - 1] + 1;
            }

            for (int idx_sim = 0; idx_sim < numSim; idx_sim++) 
            {
                
                rand = new Random(randSeedList[idx_sim]);

                // Configuration: surveillance cameras
                // constant
                int N_CCTV = param_N_CCTV;
                int N_Ped = param_N_PED;

                //Random rand = new Random(randSeed); // modified by 0boo 23-01-27

                const double Lens_FocalLength = 2.8; // mm, [2.8 3.6 6 8 12 16 25]
                const double WD = 4.8; // (mm) width, horizontal size of camera sensor
                const double HE = 3.6; // (mm) height, vertical size of camera sensor

                // const double Diag = Math.Sqrt(WD*WD + HE*HE), diagonal size
                const double imW = 1920; // (pixels) image width
                const double imH = 1080; // (pixels) image height

                const double cctv_rotate_degree = -1; //90; --> 30초에 한바퀴?, -1: angle이 회전하는 옵션 disable (note 23-01-16)
                                                      // Installation [line_23]
                const double Angle_H = 0; // pi/2, (deg), Viewing Angle (Horizontal Aspects)
                const double Angle_V = 0; // pi/2, (deg), Viewing Angle (Vertical Aspects)

                // configuration: road
                const int Road_WD = 5000; // 이거 안쓰는 변수? Road_Width 존재
                bool On_Road_Builder = true; // 0:No road, 1:Grid

                int Road_Width = 0;
                int Road_Interval = 0;
                int Road_N_Interval = 0;

                Console.WriteLine("\n\nRepetition No. {0}", idx_sim);
                if (args.Length > 0)
                {
                    Sim_ID = args[0];
                    N_CCTV = int.Parse(args[1]);
                    N_Ped  = int.Parse(args[2]);

                    Console.WriteLine(Sim_ID);
                    Console.WriteLine("N_CCTV = {0}", N_CCTV);
                    Console.WriteLine("N_Ped = {0}", N_Ped);
                }
                
                if (On_Road_Builder)
                {
                    // set 1
                    //Road_Width = 2000;// 10000; // mm
                    //Road_Interval = 10000;//88000; // mm, 10 meter
                    //Road_N_Interval = 3;//5;

                    // set 2
                    Road_Width = 10000;// 1000; // mm
                    Road_Interval = 25000;//88000; // mm, 10 meter
                    Road_N_Interval = 5;
                }


                double[] log_PED_position = null;
                if (Opt_Demo)
                {

                    StreamWriter writer;
                    writer = File.CreateText("log_PED_Position.out");
                    writer.Flush();
                    writer.Close();

                }
                // time check start
                // double accTime = 0.0;

                // ped csv file 출력 여부
                bool createPedCSV = false;

                double rotateTerm = 30.0; // sec

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                // Step 1-2) calculate vertical/horizontal AOV , (23-02-02) modifed by 0Boo, deg -> rad
                double H_AOV = 2 * Math.Atan(WD / (2 * Lens_FocalLength));//RadToDeg(2 * Math.Atan(WD / (2 * Lens_FocalLength))); // Horizontal AOV
                double V_AOV = 2 * Math.Atan(HE / (2 * Lens_FocalLength));//RadToDeg(2 * Math.Atan(HE / (2 * Lens_FocalLength))); // Vertical AOV

                // double D_AOV = RadToDeg(2 * Math.Atan(Diag / (2 * Lens_FocalLength)));
                // (mm) distance
                // double[] Dist = new double[10000];
                // int dist_len = 100000;
                // double[] Height = new double[10000];
                // for (int i = 0; i < 10000; i++)
                // {
                //     Dist[i] = i;
                //     Height[i] = i;
                // }
                int MaxLengthDistAndHeight = 25000; // (mm)
                double[] Dist = new double[MaxLengthDistAndHeight + 1];
                int dist_len = 100000;
                double[] Height = new double[MaxLengthDistAndHeight + 1];
                for (int i = 0; i < MaxLengthDistAndHeight; i++)
                {
                    Dist[i] = i;
                    Height[i] = i;
                }

                // Configuration: Pedestrian (Target Object)
                const int Ped_Width = 900; // (mm)
                const int Ped_Height = 1700; // (mm)
                const int Ped_Velocity = 1700; // (mm/s)

                cctvs = new CCTV[N_CCTV];
                for (int i = 0; i < N_CCTV; i++)
                {
                    cctvs[i] = new CCTV();
                }
                peds = new Pedestrian[N_Ped];
                for (int i = 0; i < N_Ped; i++)
                {
                    peds[i] = new Pedestrian();
                }


                /* -------------------------------------------
                *  도로 정보 생성 + 보행자/CCTV 초기화 시작
                ------------------------------------------- */
                // time check


                if (On_Road_Builder)
                {
                    // 도로 정보 생성, 보행자 정보 생성
                    road.roadBuilder(Road_Width, Road_Interval, Road_N_Interval, N_CCTV, N_Ped);

                    /*
                    // debug 220428
                    for(int i = 0 ; i < N_CCTV; i++) {
                        Console.Write(cctvs[i].X);
                      Console.Write(", ");
                      Console.WriteLine(cctvs[i].Y);

                    }
                    */
                    //road.printRoadInfo();


                    /*

                    //*  보행자, cctv 초기 설정
                    for (int i = 0; i < N_Ped; i++)
                    {
                        Console.WriteLine("{0}번째 보행자 = ({1}, {2}) ", i + 1, peds[i].X, peds[i].Y);
                    }
                    Console.WriteLine("\n============================================================\n");
                    for (int i = 0; i < N_CCTV; i++)
                    {
                        Console.WriteLine("{0}번째 cctv = ({1}, {2}) ", i + 1, cctvs[i].X, cctvs[i].Y);
                    }
                    */


                    //ped init
                    for (int i = 0; i < N_Ped; i++)
                    {
                        double minDist = 0.0;
                        //int idx_minDist = 0;
                        //double[] Dist_Map = new double[road.DST.GetLength(0)];

                        // 맨처음 위치에서 가장 가까운 도착지를 설정 (보행자 맨처음 위치는 setPed()로 설정)
                        //double[,] newPos = road.getPointOfAdjacentRoad(road.getIdxOfIntersection(ped.X, ped.Y));
                        //double dst_x = Math.Round(newPos[0, 0]);
                        //double dst_y = Math.Round(newPos[0, 1]);

                        // Car object일경우 가까운 도착지 설정
                        // double[,] newPos = road.getPointOfAdjacentIntersection(road.getIdxOfIntersection(ped.X, ped.Y), ped.X, ped.Y);
                        // double dst_x = Math.Round(newPos[0, 0]);
                        // double dst_y = Math.Round(newPos[0, 1]);

                        //Calc_Dist_and_get_MinDist(road.DST, ped.X, ped.Y, ref Dist_Map, ref minDist, ref idx_minDist);

                        //double dst_x = road.DST[idx_minDist, 0];
                        //double dst_y = road.DST[idx_minDist, 1];

                        // 보행자~목적지 벡터
                        /*
                        double[] A = new double[2];
                        A[0] = dst_x - ped.X;
                        A[1] = dst_y - ped.Y;        

                        double[] B = { 0.001, 0 };
                        double direction = Math.Round(Math.Acos(InnerProduct(A, B) / (Norm(A) * Norm(B))),8);
                        if(ped.Y > dst_y)
                        {
                            direction = Math.Round(2 * Math.PI - direction, 8); 
                        }
                        */
                        double Target_DST_X = road.DST[0, 0];
                        double Target_DST_Y = road.DST[0, 1];
                        double minDIST = road.DIST_PED_DST[i, 0];
                        for (int j = 1; j < road.DST.GetLength(0); j++)
                        {
                            if (road.DIST_PED_DST[i, j] < minDIST)
                            {
                                minDIST = road.DIST_PED_DST[i, j];
                                Target_DST_X = road.DST[j, 0];
                                Target_DST_Y = road.DST[j, 1];
                            }

                        }
                        peds[i].define_PED(Ped_Width, Ped_Height, Target_DST_X, Target_DST_Y, Ped_Velocity, N_CCTV);
                        //ped.updateDestination();   
                        peds[i].setDirection();
                        peds[i].TTL = (int)Math.Ceiling((minDist / peds[i].Velocity) / aUnitTime);
                        //peds[i].printPedInfo();
                    }

                    // cctv init
                    for (int i = 0; i < N_CCTV; i++)
                    {
                        // 220317
                        // Height.Max() 는 고정값 (=대충 10000)..
                        // 상수로 바꿔도 될듯??
                        // default Z는 3000
                        // 3000 ~ 10000 사이 값, 즉 7000이 변하는 값
                        // default(min) : 3000, variant : 7000 
                        // maxZ = min + variant 이런식으로?..

                        // cctvs[i].Z =
                        //     (int)Math.Ceiling(rand.NextDouble() * (Height.Max() - 3000)) + 3000; // milimeter

                        //cctvs[i].setZ((int)Math.Ceiling(rand.NextDouble() * (Height.Max() - 3000)) + 6000);
                        cctvs[i].setZ(10000);
                        cctvs[i].WD = WD;
                        cctvs[i].HE = HE;
                        cctvs[i].imW = (int)imW;
                        cctvs[i].imH = (int)imH;
                        cctvs[i].Focal_Length = Lens_FocalLength;
                        // 220104 초기 각도 설정
                        // cctvs[i].ViewAngleH = rand.NextDouble() * 360;
                        // cctvs[i].ViewAngleV = -35 - 20 * rand.NextDouble();
                        double Target_DST_X = road.DST[0, 0];
                        double Target_DST_Y = road.DST[0, 1];
                        double minDIST = road.DIST_CCTV_DST[i, 0];
                        for (int j = 1; j < road.DST.GetLength(0); j++)
                        {
                            if (road.DIST_CCTV_DST[i, j] < minDIST)
                            {
                                minDIST = road.DIST_CCTV_DST[i, j];
                                Target_DST_X = road.DST[j, 0];
                                Target_DST_Y = road.DST[j, 1];
                            }

                        }

                        //cctvs[i].setViewAngleH(rand.NextDouble() * 360*Math.PI/180);  // (23-02-02) modified by 0BoO, deg -> rad
                        cctvs[i].setViewAngleH(Target_DST_X, Target_DST_Y);


                        // cctvs[i].setViewAngleH(rand.Next(4) * 90);
                        // cctvs[i].setViewAngleV(-35 - 20 * rand.NextDouble());
                        cctvs[i].setViewAngleV(-45.0 * Math.PI / 180);   // (23-02-02) modified by 0BoO, deg -> rad


                        cctvs[i].setFixMode(true); // default (rotate)

                        cctvs[i].H_AOV = 2 * Math.Atan(WD / (2 * Lens_FocalLength));
                        cctvs[i].V_AOV = 2 * Math.Atan(HE / (2 * Lens_FocalLength));

                        cctvs[i].calcBlindToPed();          // (23-02-01) added by 0BoO
                        cctvs[i].calcEffDistToPed(3000);     // (23-02-01) added by 0BoO, input value is 3000mm(3meter)

                        // 기기 성능상의 최대 감시거리 (임시값)
                        cctvs[i].Max_Dist = cctvs[i].Eff_Dist_To;//50 * 100 * 10; // 50m (milimeter)
                                                                 // cctvs[i].Max_Dist = 500 * 100 * 100; // 500m (milimeter)
                        int L_Dist = (int)(cctvs[i].Eff_Dist_To - cctvs[i].Eff_Dist_From);
                        double[] Dist2 = new double[L_Dist];

                        for (int j = 1; j < L_Dist; j++)
                        {
                            Dist2[j] = cctvs[i].Eff_Dist_From + j;
                        }

                        cctvs[i].detectedPedIndex = new List<int>();

                        // Line 118~146
                        /*  여기부턴 Road_Builder 관련 정보가 없으면 의미가 없을거같아서 주석처리했어용..
                            그리고 get_Sectoral_Coverage 이런함수도 지금은 구현해야할지 애매해서..?
                        */

                        cctvs[i]
                            .get_PixelDensity(Dist2,
                            cctvs[i].WD,
                            cctvs[i].HE,
                            cctvs[i].Focal_Length,
                            cctvs[i].imW,
                            cctvs[i].imH);

                        cctvs[i].get_H_FOV(Dist2, cctvs[i].WD, cctvs[i].Focal_Length, cctvs[i].ViewAngleH, cctvs[i].X, cctvs[i].Y);
                        cctvs[i].get_V_FOV(Dist2, cctvs[i].HE, cctvs[i].Focal_Length, cctvs[i].ViewAngleV, cctvs[i].X, cctvs[i].Z);
                        // cctvs[i].printCCTVInfo();

                        //cctvs[i].calcBlindToPed();          // (23-02-01) added by 0BoO
                        //cctvs[i].calcEffDistToPed(3000);     // (23-02-01) added by 0BoO, input value is 3000mm(3meter)
                    }
                }
                /* -------------------------------------------
                *  도로 정보 생성 + 보행자/CCTV 초기화 끝
                ------------------------------------------- */

                double Sim_Time = 600; // unit: sec
                double Now = 0;

                // Console.WriteLine(">>> Simulating . . . \n");
                int[] R_Surv_Time = new int[N_Ped]; // 탐지 
                int[] directionError = new int[N_Ped]; // 방향 미스
                int[] outOfRange = new int[N_Ped]; // 거리 범위 밖
                double[] minSpatialResolution = new double[N_Ped];  // the cummulative spatial resolution(min)
                double[] maxSpatialResolution = new double[N_Ped];  // the cummulative spatial resolution(max)

                string[] traffic_x = new string[(int)(Sim_Time / aUnitTime)]; // csv 파일 출력 위한 보행자별 x좌표
                string[] traffic_y = new string[(int)(Sim_Time / aUnitTime)]; // csv 파일 출력 위한 보행자별 y좌표
                string[] detection = new string[(int)(Sim_Time / aUnitTime)]; // csv 파일 출력 위한 추적여부
                string header = "";

                int road_min = 0;
                int road_max = road.mapSize;

                // Console.WriteLine("simulatioin start: ");
                // simulation
                while (Now <= Sim_Time)
                {
                    //Console.WriteLine(".");
                    // 추적 검사
                    int[] res = checkDetection_ParFor(N_CCTV, N_Ped);
                    // threading.. error
                    // int[] res = new int[N_Ped];

                    // Thread ThreadForWork = new Thread( () => { res = checkDetection(N_CCTV, N_Ped); });     
                    // ThreadForWork.Start();

                    //(NOTE 23-05-24) parfor 처리함
                    //for (int i = 0; i < res.Length; i++)
                    //{
                    //    detection[i] += Convert.ToString(res[i]) + ",";

                    //    if (res[i] == 0) outOfRange[i]++;
                    //    else if (res[i] == -1) { outOfRange[i]++; directionError[i]++; }
                    //    else if (res[i] == 1) R_Surv_Time[i]++;
                    //}
                    Parallel.For(0, res.Length, i =>
                    {
                        detection[i] += Convert.ToString(res[i]) + ",";

                        if (res[i] == 0)
                        {
                            Interlocked.Increment(ref outOfRange[i]);
                        }
                        else if (res[i] == -1)
                        {
                            Interlocked.Increment(ref outOfRange[i]);
                            Interlocked.Increment(ref directionError[i]);
                        }
                        else if (res[i] == 1)
                        {
                            Interlocked.Increment(ref R_Surv_Time[i]);
                        }
                    });
                    if (Opt_Log)
                    {
                        //DateTime now = DateTime.Now; // 현재 시간 얻기
                        //Console.WriteLine("현재 시간: " + now.ToString()); // 현재 시간 출력

                        StreamWriter writer;
                        writer = File.AppendText("log_Events.out");

                        for (int j = 0; j < N_Ped; j++)
                        {
                            for (int i = 0; i < N_CCTV; i++)
                            {
                                if (peds[j].Spatial_Resolution[i, 0] != 0)
                                {
                                    writer.WriteLine("{0:F5} {1:D} {2:D} {3:D} {4:F3} {5:F3} {6:F3} {7:F3} {8:F3} {9:F3} {10:F3} {11:F3} {12:F3} {13:F3}",
                                        Now, j, i
                                        , (int)peds[j].Spatial_Resolution[i, 0]
                                        , peds[j].Spatial_Resolution[i, 1]
                                        , peds[j].Spatial_Resolution[i, 2]
                                        , peds[j].Spatial_Resolution[i, 3]
                                        , peds[j].Spatial_Resolution[i, 4]
                                        , peds[j].Spatial_Resolution[i, 5]
                                        , peds[j].Spatial_Resolution[i, 6]
                                        , peds[j].Spatial_Resolution[i, 7]
                                        , peds[j].Spatial_Resolution[i, 8]
                                        , peds[j].Spatial_Resolution[i, 9]
                                        , peds[j].Spatial_Resolution[i, 10]);


                                }


                            }
                        }
                        writer.Close();
                    }

                    for (int j = 0; j < N_Ped; j++)
                    {
                        double Temp_min_SR = peds[j].Spatial_Resolution[0, 9];
                        double Temp_max_SR = peds[j].Spatial_Resolution[0, 10];

                        for (int i = 1; i < N_CCTV; i++)
                        {
                            

                            if (peds[j].Spatial_Resolution[i, 0] != 0)
                            {
                                if (peds[j].Spatial_Resolution[i, 9] < Temp_min_SR)
                                {
                                    Temp_min_SR = peds[j].Spatial_Resolution[i, 9];
                                }
                                
                                if (peds[j].Spatial_Resolution[i, 10] > Temp_max_SR)
                                {
                                    Temp_max_SR = peds[j].Spatial_Resolution[i, 10];
                                }

                            }


                        }

                        minSpatialResolution[j] += Temp_min_SR;
                        maxSpatialResolution[j] += Temp_max_SR;
                    }
                    /* 220407 
                     * 보행자 방향 따라 CCTV 회전 제어
                     * 각 보행자가 탐지/미탐지 여부를 넘어서
                     * 특정 CCTV가 지금 탐지한 보행자의 정보를 알아야함
                     * 그래야 보행자의 범위 내 위치, 방향을 읽어서
                     * 보행자의 이동 방향으로 CCTV 회전 여부, 회전 시 방향 및 각도 설정 가능
                    */

                    // 이동, (NOTE 23-05-04) parfor 사용에 따라 동일 기능을 하는 블록을 제거함
                    //for (int i = 0; i < peds.Length; i++)
                    //{
                    //    if (peds[i].X < road_min || peds[i].X > road_max)
                    //    {
                    //        traffic_x[i] += "Out of range,";
                    //    }
                    //    else
                    //    {
                    //        traffic_x[i] += Math.Round(peds[i].X, 2) + ",";
                    //    }

                    //    if (peds[i].Y < road_min || peds[i].Y > road_max)
                    //    {
                    //        traffic_y[i] += "Out of range,";
                    //    }
                    //    else
                    //    {
                    //        traffic_y[i] += Math.Round(peds[i].Y, 2) + ",";
                    //    }

                    //    peds[i].move();
                    //}

                    // (NOTE 23-05-04) parfor 사용으로 변경함
                    Parallel.For(0, peds.Length, i =>
                    {
                        if (peds[i].X < road_min || peds[i].X > road_max)
                        {
                            traffic_x[i] += "Out of range,";
                        }
                        else
                        {
                            traffic_x[i] += Math.Round(peds[i].X, 2) + ",";
                        }

                        if (peds[i].Y < road_min || peds[i].Y > road_max)
                        {
                            traffic_y[i] += "Out of range,";
                        }
                        else
                        {
                            traffic_y[i] += Math.Round(peds[i].Y, 2) + ",";
                        }

                        peds[i].move();
                    });

                    // 220317 cctv rotation
                    if (cctv_rotate_degree > 0)
                    {
                        for (int i = 0; i < N_CCTV; i++)
                        {
                            // 220331 rotate 후 fov 재계산
                            // 30초마다 한바퀴 돌도록 -> 7.5초마다 90도
                            // Now는 현재 simulation 수행 경과 시간
                            // 360/cctv_rotate_degree = 4
                            // 30/4 = 7.5
                            if (Math.Round(Now, 2) % Math.Round(rotateTerm / (360.0 / cctv_rotate_degree), 2) == 0)
                            {
                                // cctv.setFixMode(false)로 설정해줘야함!
                                // Console.WriteLine("[Rotate] Now: {0}, Degree: {1}", Math.Round(Now, 2), cctvs[i].ViewAngleH);
                                cctvs[i].rotateHorizon(cctv_rotate_degree); // 90
                                                                            // 회전후 수평 FOV update (지금은 전부 Update -> 시간 오래걸림 -> 일부만(일부FOV구성좌표만)해야할듯)
                                if (!cctvs[i].isFixed)
                                    cctvs[i].get_H_FOV(Dist, cctvs[i].WD, cctvs[i].Focal_Length, cctvs[i].ViewAngleH, cctvs[i].X, cctvs[i].Y);
                            }
                        }
                    }

                    if (Opt_Observation)
                    {
                        MLApp.MLApp matlab = new MLApp.MLApp();

                        // fixed components
                        if (Now == 0)
                        {
                            matlab.Execute(@"cd 'D:\Google 드라이브\Temporary Working\연구관련\CCTV 운용 시뮬레이션\Surveillance System (C#)\2021-2_SurveillanceSystem-main\surveillance_system\src\matlab_code'");
                            //matlab.Execute(@"cd 'C:\Users\0bookim\내 드라이브\Temporary Working\연구관련\CCTV 운용 시뮬레이션\Surveillance System (C#)\2021-2_SurveillanceSystem-main\surveillance_system\src\matlab_code'");

                            //double opt_precesionBorderLine = 0.001;
                            matlab.Execute(@"clear all;");
                            matlab.Execute(@"close all;");
                            matlab.Execute(@"figure;");
                            matlab.Execute(@"hold on;");

                            for (int i = 0; i < N_CCTV; i++)
                            {
                                matlab.PutWorkspaceData("H_AOV", "base", cctvs[i].H_AOV);
                                matlab.PutWorkspaceData("ViewAngleH", "base", cctvs[i].ViewAngleH);
                                matlab.PutWorkspaceData("X", "base", cctvs[i].X);
                                matlab.PutWorkspaceData("Y", "base", cctvs[i].Y);
                                matlab.PutWorkspaceData("R_blind", "base", cctvs[i].Eff_Dist_From);
                                matlab.PutWorkspaceData("R_eff", "base", cctvs[i].Eff_Dist_To);

                                matlab.PutWorkspaceData("i", "base", i);

                                matlab.PutWorkspaceData("CCTV_H_FOV_X0", "base", cctvs[i].H_FOV.X0);
                                matlab.PutWorkspaceData("CCTV_H_FOV_X1", "base", cctvs[i].H_FOV.X1);
                                matlab.PutWorkspaceData("CCTV_H_FOV_X2", "base", cctvs[i].H_FOV.X2);

                                matlab.PutWorkspaceData("CCTV_H_FOV_Y0", "base", cctvs[i].H_FOV.Y0);
                                matlab.PutWorkspaceData("CCTV_H_FOV_Y1", "base", cctvs[i].H_FOV.Y1);
                                matlab.PutWorkspaceData("CCTV_H_FOV_Y2", "base", cctvs[i].H_FOV.Y2);


                                matlab.Execute(@"[BorderLine_blind, BorderLine_eff, X, Y] = get_Sectoral_Coverage_CS(H_AOV, ViewAngleH, X, Y, R_blind, R_eff);");
                                //matlab.Execute(@"X = cast(X,"double"); Y = cast(Y, "double");");
                                matlab.Execute(@"plot(X, Y, 'o','MarkerFaceColor','red', 'MarkerEdgeColor','Blue');");
                                matlab.Execute(@"text(X, Y, num2str(i));");

                                matlab.Execute(@"plot(CCTV_H_FOV_X0, CCTV_H_FOV_Y0, '--');");
                                matlab.Execute(@"plot(CCTV_H_FOV_X1, CCTV_H_FOV_Y1);");
                                matlab.Execute(@"plot(CCTV_H_FOV_X2, CCTV_H_FOV_Y2)");


                                matlab.Execute(@"BorderLine_blind_X(1,:) = BorderLine_blind(:,1);");
                                matlab.Execute(@"BorderLine_blind_Y(1,:) = BorderLine_blind(:,2);");

                                matlab.Execute(@"BorderLine_eff_X(1,:) = BorderLine_eff(:,1);");
                                matlab.Execute(@"BorderLine_eff_Y(1,:) = BorderLine_eff(:,2);");

                                matlab.Execute(@"plot(BorderLine_blind_X(1,:), BorderLine_blind_Y(1,:));");
                                matlab.Execute(@"plot(BorderLine_eff_X(1,:), BorderLine_eff_Y(1,:)); ");
                            }
                            int L_DST_row = road.DST.GetLength(0);
                            //int L_DST_col = road.DST.GetLength(1);

                            for (int j = 0; j < L_DST_row; j++)
                            {

                                matlab.PutWorkspaceData("DST_X", "base", road.DST[j, 0]);
                                matlab.PutWorkspaceData("DST_Y", "base", road.DST[j, 1]);
                                matlab.Execute(@"plot(DST_X, DST_Y, 'p');");

                            }
                            int L_intersection_row = road.intersectionArea.GetLength(0);
                            //int L_intersection_col = road.intersectionArea.GetLength(1);
                            for (int j = 0; j < L_intersection_row; j++)
                            {

                                matlab.PutWorkspaceData("intersection_X1", "base", road.intersectionArea[j, 0]);
                                matlab.PutWorkspaceData("intersection_Y1", "base", road.intersectionArea[j, 2]);
                                matlab.PutWorkspaceData("intersection_X2", "base", road.intersectionArea[j, 1]);
                                matlab.PutWorkspaceData("intersection_Y2", "base", road.intersectionArea[j, 3]);
                                //matlab.Execute(@"plot([intersection_X1 intersection_X2],[intersection_Y1 intersection_Y2], 'k.-');");

                            }

                            int L_roadVector = road.laneVector.Length;
                            int L_roadLaneH = road.lane_h.GetLength(0);
                            int L_roadLaneV = road.lane_v.GetLength(0);

                            matlab.PutWorkspaceData("lane_vector", "base", road.laneVector);


                            for (int h = 0; h < L_roadLaneH; h++)
                            {
                                matlab.PutWorkspaceData("lane_h", "base", road.lane_h[h, 0]);
                                matlab.PutWorkspaceData("lane_h_upper", "base", road.lane_h_upper[h, 0]);
                                matlab.PutWorkspaceData("lane_h_lower", "base", road.lane_h_lower[h, 0]);

                                matlab.Execute(@"LANE_H = ones(1,length(lane_vector))*lane_h;");
                                matlab.Execute(@"LANE_HU = ones(1,length(lane_vector))*lane_h_upper;");
                                matlab.Execute(@"LANE_HL = ones(1,length(lane_vector))*lane_h_lower;");

                                matlab.Execute(@"plot(lane_vector,LANE_H,'--');");
                                matlab.Execute(@"plot(lane_vector,LANE_HU,'-');");
                                matlab.Execute(@"plot(lane_vector,LANE_HL,'-');");
                            }

                            for (int v = 0; v < L_roadLaneV; v++)
                            {
                                matlab.PutWorkspaceData("lane_v", "base", road.lane_v[v, 0]);
                                matlab.PutWorkspaceData("lane_v_left", "base", road.lane_v_left[v, 0]);
                                matlab.PutWorkspaceData("lane_v_right", "base", road.lane_v_right[v, 0]);

                                matlab.Execute(@"LANE_V = ones(1,length(lane_vector))*lane_v;");
                                matlab.Execute(@"LANE_VL = ones(1,length(lane_vector))*lane_v_left;");
                                matlab.Execute(@"LANE_VR = ones(1,length(lane_vector))*lane_v_right;");

                                matlab.Execute(@"plot(LANE_V,lane_vector,'--');");
                                matlab.Execute(@"plot(LANE_VL,lane_vector,'-');");
                                matlab.Execute(@"plot(LANE_VR,lane_vector,'-');");
                            }

                            matlab.Execute(@"grid on;");
                            matlab.Execute(@"xlabel('X-axis(mm)');ylabel('Y-axis(mm)')");
                        }



                        // variable components
                        // pedestrians

                        for (int j = 0; j < N_Ped; j++)
                        {
                            matlab.PutWorkspaceData("Pos_H1", "base", peds[j].Pos_H1);
                            matlab.PutWorkspaceData("Pos_H2", "base", peds[j].Pos_H2);
                            matlab.PutWorkspaceData("Pos_V1", "base", peds[j].Pos_V1);
                            matlab.PutWorkspaceData("Pos_V2", "base", peds[j].Pos_V2);

                            matlab.Execute(@"plot([Pos_H1(1) Pos_H2(1)], [Pos_H1(2) Pos_H2(2)], 's-');");
                        }

                        matlab.PutWorkspaceData("Now", "base", Now);
                        matlab.Execute(@"title(['Time = ', num2str(Now), ' sec'] );");
                        matlab.Execute(@"pause(0.1);");
                        // (TBD) performance
                    }

                    if (Opt_Demo)
                    {
                        //var log_FilePath1 = @"log_PED_Position.txt";
                        //var log_result1 = new StreamWriter(log_FilePath1);
                        StreamWriter writer;
                        writer = File.AppendText("log_PED_Position.out");

                        for (int i = 0; i < N_Ped; i++)
                        {
                            writer.WriteLine("{0:F} {1:F2} {2:F2} {3:F2} {4:F2}", Now, peds[i].Pos_H1[0], peds[i].Pos_H1[1], peds[i].Pos_H2[0], peds[i].Pos_H2[1]);
                        }

                        writer.Close();
                    }

                    header += Convert.ToString(Math.Round(Now, 1)) + ",";
                    Now += aUnitTime;
                }
                stopwatch.Stop();

                // // create .csv file
                if (createPedCSV)
                {
                    for (int i = 0; i < peds.Length; i++)
                    {
                        string fileName = "ped" + i + ".csv";
                        using (System.IO.StreamWriter file = new System.IO.StreamWriter(@fileName))
                        {
                            file.WriteLine(header);
                            file.WriteLine(traffic_x[i]);
                            file.WriteLine(traffic_y[i]);
                            file.WriteLine(detection[i]);
                        }
                    }
                }
                double totalSimCount = Sim_Time / aUnitTime * N_Ped;

                double[] Avg_minSpatialResolution = new double[N_Ped];
                double[] Avg_maxSpatialResolution = new double[N_Ped];

                for (int i = 0; i < N_Ped; i++)
                {
                    Avg_minSpatialResolution[i] = minSpatialResolution[i] / R_Surv_Time[i];
                    Avg_maxSpatialResolution[i] = maxSpatialResolution[i] / R_Surv_Time[i];
                    
                }

                // 결과(탐지율)
                Console.WriteLine("====== Surveillance Time Result I ======");
                Console.WriteLine("N_CCTV: {0}, N_Ped: {1}", N_CCTV, N_Ped);
                Console.WriteLine("[Result]");
                Console.WriteLine("  - Execution time : {0}", stopwatch.ElapsedMilliseconds + "ms");
                Console.WriteLine("[Fail]");
                Console.WriteLine("  - Out of Range: {0:F2}% ({1}/{2})", 100 * outOfRange.Sum() / totalSimCount, outOfRange.Sum(), totalSimCount);
                Console.WriteLine("  - Direction Error: {0:F2}% ({1}/{2})", 100 * directionError.Sum() / totalSimCount, directionError.Sum(), totalSimCount);
                Console.WriteLine("[Success]");
                Console.WriteLine("  - Surveillance Time: {0:F2}% ({1}/{2})\n", 100 * R_Surv_Time.Sum() / totalSimCount, R_Surv_Time.Sum(), totalSimCount);

                Console.WriteLine("====== Surveillance Time Result II ======");
                Console.WriteLine(" Simulation Time (sec): {0:F2}", Sim_Time);
                Console.WriteLine(" [Avg] Out of Range (sec): {0:F2}", outOfRange.Average() * aUnitTime);
                Console.WriteLine(" [Avg] Direction Error (sec): {0:F2}", directionError.Average() * aUnitTime);
                Console.WriteLine(" [Avg] Success Time (sec): {0:F2}", R_Surv_Time.Average() * aUnitTime);
                Console.WriteLine(" [Avg] Spatial Resolution (min): {0:F2}", minSpatialResolution.Average()/ Sim_Time);
                Console.WriteLine(" [Avg] Spatial Resolution (max): {0:F2}", maxSpatialResolution.Average()/ Sim_Time);
                Console.WriteLine(" [Avg] Spatial Resolution (min): {0:F2}", Avg_minSpatialResolution.Average());
                Console.WriteLine(" [Avg] Spatial Resolution (max): {0:F2}", Avg_maxSpatialResolution.Average());

                string subFolderPath = "Sim_Results";
                string subFolderPathWithFile = Path.Combine(Directory.GetCurrentDirectory(), subFolderPath);
                if (!Directory.Exists(subFolderPathWithFile))
                {
                    Directory.CreateDirectory(subFolderPathWithFile);
                }
                string result_FilePath1 = Path.Combine(subFolderPathWithFile, Sim_ID + "_" + "Result1.out");
                string result_FilePath2 = Path.Combine(subFolderPathWithFile, Sim_ID + "_" + "Result2.out");


                //StreamWriter wt_result1 = new StreamWriter(result_FilePath1);
                //StreamWriter wt_result2 = new StreamWriter(result_FilePath2);

                using (StreamWriter wt_result1 = File.AppendText(result_FilePath1))
                {
                    wt_result1.WriteLine("{1:F6} {2:F6} {0:F6}", R_Surv_Time.Average() * aUnitTime, outOfRange.Average() * aUnitTime, directionError.Average() * aUnitTime);
                    //wt_result1.Close();
                }
                using (StreamWriter wt_result2 = File.AppendText(result_FilePath2))
                {
                    for (int i = 0; i < N_Ped; i++)
                    {
                        wt_result2.WriteLine("{3} {4} {1:F2} {2:F2} {0:F2}", R_Surv_Time[i] * aUnitTime, outOfRange[i] * aUnitTime, directionError[i] * aUnitTime, randSeedList[idx_sim], i);
                    }
                    //wt_result2.Close();
                }

                 

                // 결과(시간)
                // Console.WriteLine("Execution time : {0}", stopwatch.ElapsedMilliseconds + "ms");
                // accTime += stopwatch.ElapsedMilliseconds;

                // Console.WriteLine("\n============ RESULT ============");
                // Console.WriteLine("CCTV: {0}, Ped: {1}", N_CCTV, N_Ped);
                // Console.WriteLine("Execution time : {0}\n", (accTime / 1000.0 ) + " sec");
                string subFolderPath1 = "Sim_Results";
                string subFolderPathWithFile1 = Path.Combine(Directory.GetCurrentDirectory(), subFolderPath1);
                if (!Directory.Exists(subFolderPathWithFile1))
                {
                    Directory.CreateDirectory(subFolderPathWithFile1);
                }
                string excelFilePath = Path.Combine(subFolderPathWithFile1, "Sim_Results.xlsx");

                FileInfo excelResultFile = new FileInfo(excelFilePath);
                ExcelPackage excelPackage;

                if (excelResultFile.Exists)
                {
                    // 기존 파일이 있는 경우 로드
                    excelPackage = new ExcelPackage(excelResultFile);
                }
                else
                {
                    // 새로운 파일인 경우 생성
                    excelPackage = new ExcelPackage(excelResultFile);
                }

                // 기존 시트 확인
                ExcelWorksheet worksheet;
                if (excelPackage.Workbook.Worksheets.Any(x => x.Name == Sim_ID))
                {
                    // 기존 시트가 있는 경우 해당 시트 가져오기
                    worksheet = excelPackage.Workbook.Worksheets[Sim_ID];
                }
                else
                {
                    // 기존 시트가 없는 경우 새로운 시트 생성
                    worksheet = excelPackage.Workbook.Worksheets.Add(Sim_ID);
                }


                worksheet.Cells["A1"].Value = "seed";
                worksheet.Cells["B1"].Value = "Out of range (sec)";
                worksheet.Cells["C1"].Value = "Direction error (sec)";
                worksheet.Cells["D1"].Value = "Success (sec)";
                worksheet.Cells["E1"].Value = "minSpatialResolution";
                worksheet.Cells["F1"].Value = "maxSpatialResolution";

                int TargetCellCol = idx_sim + 2;
                worksheet.Cells["A" + TargetCellCol as string].Value = randSeedList[idx_sim]; 
                worksheet.Cells["B" + TargetCellCol as string].Value = outOfRange.Average() * aUnitTime; 
                worksheet.Cells["C" + TargetCellCol as string].Value = directionError.Average() * aUnitTime;
                worksheet.Cells["D" + TargetCellCol as string].Value = R_Surv_Time.Average() * aUnitTime;
                worksheet.Cells["E" + TargetCellCol as string].Value = minSpatialResolution.Average() / Sim_Time;
                worksheet.Cells["F" + TargetCellCol as string].Value = maxSpatialResolution.Average() / Sim_Time;

                if (idx_sim == numSim - 1)
                {
                    TargetCellCol = TargetCellCol + 1;
                    worksheet.Cells["B" + TargetCellCol as string].Formula = "AVERAGE(B2:B" + (TargetCellCol - 1) as string + ")";
                    worksheet.Cells["C" + TargetCellCol as string].Formula = "AVERAGE(C2:C" + (TargetCellCol - 1) as string + ")";
                    worksheet.Cells["D" + TargetCellCol as string].Formula = "AVERAGE(D2:D" + (TargetCellCol - 1) as string + ")";
                    worksheet.Cells["E" + TargetCellCol as string].Formula = "AVERAGE(E2:E" + (TargetCellCol - 1) as string + ")";
                    worksheet.Cells["F" + TargetCellCol as string].Formula = "AVERAGE(F2:F" + (TargetCellCol - 1) as string + ")";
                }    
                    //var file = new System.IO.FileInfo("Sim_Results.xlsx");
                excelPackage.Save();
                

                if (On_Visualization)
                {
                    MLApp.MLApp matlab = new MLApp.MLApp();

                    matlab.Execute(@"cd 'D:\Google 드라이브\Temporary Working\연구관련\CCTV 운용 시뮬레이션\Surveillance System (C#)\2021-2_SurveillanceSystem-main\surveillance_system\src\matlab_code'");
                    //matlab.Execute(@"cd 'C:\Users\0bookim\내 드라이브\Temporary Working\연구관련\CCTV 운용 시뮬레이션\Surveillance System (C#)\2021-2_SurveillanceSystem-main\surveillance_system\src\matlab_code'");

                    double[] X1;
                    double[] Y1;
                    double[] X2;
                    double[] Y2;

                    //double opt_precesionBorderLine = 0.001;
                    matlab.Execute(@"clear all;");
                    matlab.Execute(@"close all;");
                    matlab.Execute(@"figure;");
                    matlab.Execute(@"hold on;");

                    for (int i = 0; i < N_CCTV; i++)
                    {
                        matlab.PutWorkspaceData("H_AOV", "base", cctvs[i].H_AOV);
                        matlab.PutWorkspaceData("ViewAngleH", "base", cctvs[i].ViewAngleH);
                        matlab.PutWorkspaceData("X", "base", cctvs[i].X);
                        matlab.PutWorkspaceData("Y", "base", cctvs[i].Y);
                        matlab.PutWorkspaceData("R_blind", "base", cctvs[i].Eff_Dist_From);
                        matlab.PutWorkspaceData("R_eff", "base", cctvs[i].Eff_Dist_To);

                        matlab.PutWorkspaceData("i", "base", i);

                        matlab.PutWorkspaceData("CCTV_H_FOV_X0", "base", cctvs[i].H_FOV.X0);
                        matlab.PutWorkspaceData("CCTV_H_FOV_X1", "base", cctvs[i].H_FOV.X1);
                        matlab.PutWorkspaceData("CCTV_H_FOV_X2", "base", cctvs[i].H_FOV.X2);

                        matlab.PutWorkspaceData("CCTV_H_FOV_Y0", "base", cctvs[i].H_FOV.Y0);
                        matlab.PutWorkspaceData("CCTV_H_FOV_Y1", "base", cctvs[i].H_FOV.Y1);
                        matlab.PutWorkspaceData("CCTV_H_FOV_Y2", "base", cctvs[i].H_FOV.Y2);


                        matlab.Execute(@"[BorderLine_blind, BorderLine_eff, X, Y] = get_Sectoral_Coverage_CS(H_AOV, ViewAngleH, X, Y, R_blind, R_eff);");
                        //matlab.Execute(@"X = cast(X,"double"); Y = cast(Y, "double");");
                        matlab.Execute(@"plot(X, Y, 'o','MarkerFaceColor','red', 'MarkerEdgeColor','Blue');");
                        matlab.Execute(@"text(X, Y, num2str(i));");

                        matlab.Execute(@"plot(CCTV_H_FOV_X0, CCTV_H_FOV_Y0, '--');");
                        matlab.Execute(@"plot(CCTV_H_FOV_X1, CCTV_H_FOV_Y1);");
                        matlab.Execute(@"plot(CCTV_H_FOV_X2, CCTV_H_FOV_Y2)");


                        matlab.Execute(@"BorderLine_blind_X(1,:) = BorderLine_blind(:,1);");
                        matlab.Execute(@"BorderLine_blind_Y(1,:) = BorderLine_blind(:,2);");

                        matlab.Execute(@"BorderLine_eff_X(1,:) = BorderLine_eff(:,1);");
                        matlab.Execute(@"BorderLine_eff_Y(1,:) = BorderLine_eff(:,2);");

                        matlab.Execute(@"plot(BorderLine_blind_X(1,:), BorderLine_blind_Y(1,:));");
                        matlab.Execute(@"plot(BorderLine_eff_X(1,:), BorderLine_eff_Y(1,:)); ");
                    }

                    for (int j = 0; j < N_Ped; j++)
                    {
                        matlab.PutWorkspaceData("Pos_H1", "base", peds[j].Pos_H1);
                        matlab.PutWorkspaceData("Pos_H2", "base", peds[j].Pos_H2);
                        matlab.PutWorkspaceData("Pos_V1", "base", peds[j].Pos_V1);
                        matlab.PutWorkspaceData("Pos_V2", "base", peds[j].Pos_V2);

                        matlab.Execute(@"plot([Pos_H1(1) Pos_H2(1)], [Pos_H1(2) Pos_H2(2)], 's-');");
                    }

                    int L_DST_row = road.DST.GetLength(0);
                    //int L_DST_col = road.DST.GetLength(1);

                    for (int j = 0; j < L_DST_row; j++)
                    {

                        matlab.PutWorkspaceData("DST_X", "base", road.DST[j, 0]);
                        matlab.PutWorkspaceData("DST_Y", "base", road.DST[j, 1]);
                        matlab.Execute(@"plot(DST_X, DST_Y, 'p');");

                    }
                    int L_intersection_row = road.intersectionArea.GetLength(0);
                    //int L_intersection_col = road.intersectionArea.GetLength(1);
                    for (int j = 0; j < L_intersection_row; j++)
                    {

                        matlab.PutWorkspaceData("intersection_X1", "base", road.intersectionArea[j, 0]);
                        matlab.PutWorkspaceData("intersection_Y1", "base", road.intersectionArea[j, 2]);
                        matlab.PutWorkspaceData("intersection_X2", "base", road.intersectionArea[j, 1]);
                        matlab.PutWorkspaceData("intersection_Y2", "base", road.intersectionArea[j, 3]);
                        //matlab.Execute(@"plot([intersection_X1 intersection_X2],[intersection_Y1 intersection_Y2], 'k.-');");

                    }

                    int L_roadVector = road.laneVector.Length;
                    int L_roadLaneH = road.lane_h.GetLength(0);
                    int L_roadLaneV = road.lane_v.GetLength(0);

                    matlab.PutWorkspaceData("lane_vector", "base", road.laneVector);


                    for (int h = 0; h < L_roadLaneH; h++)
                    {
                        matlab.PutWorkspaceData("lane_h", "base", road.lane_h[h, 0]);
                        matlab.PutWorkspaceData("lane_h_upper", "base", road.lane_h_upper[h, 0]);
                        matlab.PutWorkspaceData("lane_h_lower", "base", road.lane_h_lower[h, 0]);

                        matlab.Execute(@"LANE_H = ones(1,length(lane_vector))*lane_h;");
                        matlab.Execute(@"LANE_HU = ones(1,length(lane_vector))*lane_h_upper;");
                        matlab.Execute(@"LANE_HL = ones(1,length(lane_vector))*lane_h_lower;");

                        matlab.Execute(@"plot(lane_vector,LANE_H,'--');");
                        matlab.Execute(@"plot(lane_vector,LANE_HU,'-');");
                        matlab.Execute(@"plot(lane_vector,LANE_HL,'-');");
                    }

                    for (int v = 0; v < L_roadLaneV; v++)
                    {
                        matlab.PutWorkspaceData("lane_v", "base", road.lane_v[v, 0]);
                        matlab.PutWorkspaceData("lane_v_left", "base", road.lane_v_left[v, 0]);
                        matlab.PutWorkspaceData("lane_v_right", "base", road.lane_v_right[v, 0]);

                        matlab.Execute(@"LANE_V = ones(1,length(lane_vector))*lane_v;");
                        matlab.Execute(@"LANE_VL = ones(1,length(lane_vector))*lane_v_left;");
                        matlab.Execute(@"LANE_VR = ones(1,length(lane_vector))*lane_v_right;");

                        matlab.Execute(@"plot(LANE_V,lane_vector,'--');");
                        matlab.Execute(@"plot(LANE_VL,lane_vector,'-');");
                        matlab.Execute(@"plot(LANE_VR,lane_vector,'-');");
                    }

                    matlab.Execute(@"grid on;");
                    matlab.Execute(@"xlabel('X-axis(mm)');ylabel('Y-axis(mm)')");
                    matlab.Execute(@"hold off;");
                    //MLApp.MLApp matlab = new MLApp.MLApp();
                    //matlab.Execute(@"figure;");
                    //matlab.Execute(@"plot(0:0.01:pi, sin(0:0.01:pi))");

                }

                if (Opt_Demo)
                {
                    MLApp.MLApp matlab = new MLApp.MLApp();
                    //matlab.Execute(@"cd 'D:\Google 드라이브\Temporary Working\연구관련\CCTV 운용 시뮬레이션\Surveillance System (C#)\2021-2_SurveillanceSystem-main\surveillance_system\src\matlab_code'");
                    matlab.Execute(@"cd 'C:\Users\0bookim\내 드라이브\Temporary Working\연구관련\CCTV 운용 시뮬레이션\Surveillance System (C#)\2021-2_SurveillanceSystem-main\surveillance_system\src\matlab_code'");
                    matlab.Execute(@"Sim_Demo");
                }
            }

            if (args.Length == 0) 
            {
                Console.Beep(1000, 1000);
                Console.Beep(1000, 1000);
                Console.Beep(1000, 1000);
                Console.Beep(1000, 1000);
                Console.Beep(1000, 1000);
            }
            
        }
    }
}
