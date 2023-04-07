Log_PedPos = load('C:\Users\0bookim\내 드라이브\Temporary Working\연구관련\CCTV 운용 시뮬레이션\Surveillance System (C#)\2021-2_SurveillanceSystem-main\surveillance_system\bin\Debug\netcoreapp3.1\log_PED_Position.out');


L_log = length(Log_PedPos);
figure;
for i = 1:L_log
    hold on;
    plot( [ Log_PedPos(i,2) Log_PedPos(i,3) ], [ Log_PedPos(i,4) Log_PedPos(i,5)], 's-');
    title(['Time = ', num2str(Log_PedPos(i,1)), ' (sec)']);
    hold off;
    pause(0.02);
end


