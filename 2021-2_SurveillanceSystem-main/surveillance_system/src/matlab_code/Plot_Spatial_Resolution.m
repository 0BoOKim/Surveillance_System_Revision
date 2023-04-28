close all;
clear all;

X = load('D:\Google 드라이브\Temporary Working\연구관련\CCTV 운용 시뮬레이션\Surveillance System (C#)\2021-2_SurveillanceSystem-main\surveillance_system\bin\Debug\netcoreapp3.1\log_Events.out');

Target_PED = 1;
Target_Event = [1 -1];
R = [];

for i = 1:length(X)
    if X(i, 2) == Target_PED && ismember(X(i, 4),Target_Event)
        R = [R; X(i,1) X(i,3) X(i,13) X(i,4)];
    end
    
end

List_CCTV = unique(R(:,2));

R2 = zeros(length(unique(R(:,1))), length(List_CCTV));
R2(:,1) = unique(R(:,1));
idx = 1;
base_time = R2(idx,1);

for i = 1:length(R)
    if R(i,1) == base_time

    elseif R(i,1) ~= base_time
        idx = idx + 1;
        base_time = R2(idx,1);       
    end
    
   for j=1:length(List_CCTV)
       if R(i, 2) == List_CCTV(j)
          R2(idx, j+1) = R(i, 3);
       end
   end
end

figure;
hold on;
legend_str = {};
for i = 1:length(List_CCTV)
   plot(R2(:,1), R2(:,i+1)); 
   legend_str{i} = strcat('Surv. Cam. #', num2str(List_CCTV(i)));
end
grid on;
xlabel('time(sec)');
ylabel('number of pixels 2');
legend(legend_str,'Location', 'Best');
hold off;

figure;
hold on;
legend_str = {};
for i = 1:length(List_CCTV)
   plot(R2(:,1), sqrt(R2(:,i+1))); 
   legend_str{i} = strcat('Surv. Cam. #', num2str(List_CCTV(i)));
end
grid on;
xlabel('time(sec)');
ylabel('number of pixels');
legend(legend_str,'Location', 'Best');
hold off;

figure;
hold on;
for i = 1:length(List_CCTV)
    ecdf(R2(:,i+1));
end
grid on;
legend(legend_str,'Location', 'Best');
hold off;